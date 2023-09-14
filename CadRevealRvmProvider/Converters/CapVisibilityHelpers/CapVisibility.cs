namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

using CadRevealComposer.Utils;
using RvmSharp.Primitives;
using System.Numerics;

public static class CapVisibility
{
    public static int TotalNumberOfCapsTested;
    public static int CapsHidden;
    public static int CapsShown;
    public static int CapsWithoutConnections;

    /// <summary>
    /// Allows caps to be hidden even if it is a bit larger than the connected cap.
    /// This value should be so low that it doesn't impact user experience by hiding caps that are clearly visible
    /// </summary>
    public static readonly float CapOverlapTolerance = 0.01f; // 0.01 is 1 cm

    /// <summary>
    /// Checks cap visibility on RvmPrimitives with one cap
    /// </summary>
    /// <param name="primitive"></param>
    /// <param name="capCenter"></param>
    /// <returns></returns>
    public static bool IsCapVisible(RvmPrimitive primitive, Vector3 capCenter)
    {
        TotalNumberOfCapsTested--; // Subtracting one, since two will be counted later
        CapsShown--; // Subtracting one, since "CapB" will be counted as shown later

        return IsCapsVisible(primitive, capCenter, Vector3.Zero).showCapA;
    }

    /// <summary>
    /// Checks cap visibility on RvmPrimitives with two caps
    /// </summary>
    /// <param name="primitive"></param>
    /// <param name="capCenterA"></param>
    /// <param name="capCenterB"></param>
    /// <returns></returns>
    public static (bool showCapA, bool showCapB) IsCapsVisible(
        RvmPrimitive primitive,
        Vector3 capCenterA,
        Vector3 capCenterB
    )
    {
        TotalNumberOfCapsTested += 2;
        CountNoConnections(primitive, capCenterB != Vector3.Zero);

        bool mustShowCapA = true;
        bool mustShowCapB = true;

        foreach (var connection in primitive.Connections.WhereNotNull())
        {
            // sort Primitive1/Primitive2 to avoid creating double amount of switch statements
            bool isSorted =
                StringComparer.Ordinal.Compare(
                    connection.Primitive1.GetType().Name,
                    connection.Primitive2.GetType().Name
                ) < 0;

            var prim1 = isSorted ? connection.Primitive1 : connection.Primitive2;
            var prim2 = isSorted ? connection.Primitive2 : connection.Primitive1;

            var connectionIndex1 = isSorted ? connection.ConnectionIndex1 : connection.ConnectionIndex2;
            var connectionIndex2 = isSorted ? connection.ConnectionIndex2 : connection.ConnectionIndex1;

            var isPrim1CurrentPrimitive = ReferenceEquals(primitive, prim1);

            var diffA = Vector3.Distance(connection.Position, capCenterA);
            var diffB = Vector3.Distance(connection.Position, capCenterB);

            var isCapCenterA = diffA <= diffB;
            var isCapCenterB = diffB < diffA;

            CapData<T> CreateCapData<T>(T node, uint capIndex, bool isCurrentPrimitive)
                where T : RvmPrimitive
            {
                return new CapData<T>(node, capIndex, isCurrentPrimitive);
            }

            bool showCap = (prim1, prim2) switch
            {
                (RvmBox box, RvmCylinder cylinder)
                    => BoxCylinderComparer.ShowCap(
                        CreateCapData(box, connectionIndex1, isPrim1CurrentPrimitive),
                        CreateCapData(cylinder, connectionIndex2, !isPrim1CurrentPrimitive)
                    ),
                (RvmBox box, RvmSnout snout)
                    => BoxSnoutComparer.ShowCap(
                        CreateCapData(box, connectionIndex1, isPrim1CurrentPrimitive),
                        CreateCapData(snout, connectionIndex2, !isPrim1CurrentPrimitive)
                    ),
                (RvmCylinder cylinder1, RvmCylinder cylinder2)
                    => CylinderCylinderComparer.ShowCap(
                        CreateCapData(cylinder1, connectionIndex1, isPrim1CurrentPrimitive),
                        CreateCapData(cylinder2, connectionIndex2, !isPrim1CurrentPrimitive)
                    ),
                (RvmCircularTorus torus1, RvmCircularTorus torus2)
                    => TorusTorusComparer.ShowCap(
                        CreateCapData(torus1, connectionIndex1, isPrim1CurrentPrimitive),
                        CreateCapData(torus2, connectionIndex2, !isPrim1CurrentPrimitive)
                    ),
                (RvmCircularTorus torus, RvmCylinder cylinder)
                    => TorusCylinderComparer.ShowCap(
                        CreateCapData(torus, connectionIndex1, isPrim1CurrentPrimitive),
                        CreateCapData(cylinder, connectionIndex2, !isPrim1CurrentPrimitive)
                    ),
                (RvmCircularTorus torus, RvmSnout snout)
                    => TorusSnoutComparer.ShowCap(
                        CreateCapData(torus, connectionIndex1, isPrim1CurrentPrimitive),
                        CreateCapData(snout, connectionIndex2, !isPrim1CurrentPrimitive)
                    ),
                (RvmCylinder cylinder, RvmSphericalDish dish)
                    => CylinderSphericalDishComparer.ShowCap(
                        CreateCapData(cylinder, connectionIndex1, isPrim1CurrentPrimitive),
                        CreateCapData(dish, connectionIndex2, !isPrim1CurrentPrimitive)
                    ),
                (RvmCylinder cylinder, RvmEllipticalDish dish)
                    => CylinderEllipticalDishComparer.ShowCap(
                        CreateCapData(cylinder, connectionIndex1, isPrim1CurrentPrimitive),
                        CreateCapData(dish, connectionIndex2, !isPrim1CurrentPrimitive)
                    ),
                (RvmCylinder cylinder, RvmSnout snout)
                    => CylinderSnoutComparer.ShowCap(
                        CreateCapData(cylinder, connectionIndex1, isPrim1CurrentPrimitive),
                        CreateCapData(snout, connectionIndex2, !isPrim1CurrentPrimitive)
                    ),
                (RvmEllipticalDish dish, RvmSnout snout)
                    => EllipticalDishSnoutComparer.ShowCap(
                        CreateCapData(dish, connectionIndex1, isPrim1CurrentPrimitive),
                        CreateCapData(snout, connectionIndex2, !isPrim1CurrentPrimitive)
                    ),
                (RvmSnout snout1, RvmSnout snout2)
                    => SnoutSnoutComparer.ShowCap(
                        CreateCapData(snout1, connectionIndex1, isPrim1CurrentPrimitive),
                        CreateCapData(snout2, connectionIndex2, !isPrim1CurrentPrimitive)
                    ),
                (RvmSnout snout, RvmSphericalDish dish)
                    => SnoutSphericalDishComparer.ShowCap(
                        CreateCapData(snout, connectionIndex1, isPrim1CurrentPrimitive),
                        CreateCapData(dish, connectionIndex2, !isPrim1CurrentPrimitive)
                    ),
                _ => true
            };

            if (showCap is false && isCapCenterA)
            {
                mustShowCapA = false;
            }

            if (showCap is false && isCapCenterB)
            {
                mustShowCapB = false;
            }
        }

        if (mustShowCapA)
            CapsShown++;
        else
            CapsHidden++;
        if (mustShowCapB)
            CapsShown++;
        else
            CapsHidden++;

        return (mustShowCapA, mustShowCapB);
    }

    private static void CountNoConnections(RvmPrimitive primitive, bool hasTwoCaps)
    {
        if (primitive.Connections.All(x => x == null))
        {
            CapsWithoutConnections++;
            if (hasTwoCaps)
            {
                CapsWithoutConnections++;
            }
        }
        else
        {
            var allConnections = primitive.Connections.WhereNotNull().ToArray();
            if (allConnections.Length > 1)
            {
                var firstItem = allConnections.First();
                bool allTheSame = allConnections.All(x => x.Position.Equals(firstItem.Position));

                if (allTheSame)
                    CapsWithoutConnections++; // If all connections are the same, assume that one cap does not have a connection
            }
            else
            {
                CapsWithoutConnections++; // Just one connection means that one cap does not have a connection
            }
        }
    }
}
