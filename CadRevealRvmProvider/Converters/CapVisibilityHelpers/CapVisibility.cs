﻿namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

using CadRevealComposer.Utils;
using RvmSharp.Primitives;
using System.Numerics;

public static class CapVisibility
{
    public static int TotalNumberOfCapsTested;
    public static int CapsHidden;
    public static int CapsShown;
    public static int CapsWithoutConnections;

    public static readonly float CapComparingBuffer = 0.01f;

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
            var isSorted =
                StringComparer.Ordinal.Compare(
                    connection.Primitive1.GetType().Name,
                    connection.Primitive2.GetType().Name
                ) < 0;

            var prim1 = isSorted ? connection.Primitive1 : connection.Primitive2;
            var prim2 = isSorted ? connection.Primitive2 : connection.Primitive1;

            var connectionIndex1 = isSorted ? connection.ConnectionIndex1 : connection.ConnectionIndex2;
            var connectionIndex2 = isSorted ? connection.ConnectionIndex2 : connection.ConnectionIndex1;

            var isPrim1CurrentPrimitive = ReferenceEquals(primitive, prim1);

            var capData1 = new CapData(prim1, connectionIndex1, isPrim1CurrentPrimitive);
            var capData2 = new CapData(prim2, connectionIndex2, !isPrim1CurrentPrimitive);

            var diffA = Vector3.Distance(connection.Position, capCenterA);
            var diffB = Vector3.Distance(connection.Position, capCenterB);

            var isCapCenterA = diffA <= diffB;
            var isCapCenterB = diffB < diffA;

            ICapComparer? capComparer = (capData1.Primitive, capData2.Primitive) switch
            {
                (RvmBox, RvmCylinder) => new BoxCylinderComparer(),
                (RvmBox, RvmSnout) => new BoxSnoutComparer(),
                (RvmCylinder, RvmCylinder) => new CylinderCylinderComparer(),
                (RvmCircularTorus, RvmCircularTorus) => new TorusTorusComparer(),
                (RvmCircularTorus, RvmCylinder) => new TorusCylinderComparer(),
                (RvmCircularTorus, RvmSnout) => new TorusSnoutComparer(),
                (RvmCylinder, RvmSphericalDish) => new CylinderSphericalDishComparer(),
                (RvmCylinder, RvmEllipticalDish) => new CylinderEllipticalDishComparer(),
                (RvmCylinder, RvmSnout) => new CylinderSnoutComparer(),
                (RvmEllipticalDish, RvmSnout) => new EllipticalDishSnoutComparer(),
                (RvmSnout, RvmSnout) => new SnoutSnoutComparer(),
                (RvmSnout, RvmSphericalDish) => new SnoutSphericalDishComparer(),
                _ => null
            };

            bool showCap = capComparer?.ShowCap(capData1, capData2) ?? true;

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