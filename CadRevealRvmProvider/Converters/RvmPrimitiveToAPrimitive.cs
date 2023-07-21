namespace CadRevealRvmProvider.Converters;

using CadRevealComposer.Primitives;
using RvmSharp.Primitives;
using System.Drawing;
using System.Runtime.CompilerServices;

public class RvmPrimitiveToAPrimitive
{
    public static IEnumerable<APrimitive> FromRvmPrimitive(
        ulong treeIndex,
        RvmPrimitive rvmPrimitive,
        RvmNode rvmNode, //Todo: We only use color???!  Simplify?
        Dictionary<string, string> attributes,
        bool isShadow
    )
    {
        if (rvmNode == null)
        {
            Console.WriteLine(
                $"The RvmGroup for Node {treeIndex} was invalid: {rvmNode?.GetType()}. Returning empty array."
            );
            return Array.Empty<APrimitive>();
        }
        //if (rvmNode.Name.Contains("HELIDECK-DECK"))
        //{
        //    //Console.WriteLine($"Got '{rvmNode.Name}' - {attributes["RefNo"]}");
        //}
        ////else
        ////    return Array.Empty<APrimitive>();

        //if (isShadow && attributes?.Count > 0)
        //{
        //    if (rvmNode.Name.Contains("HELIDECK-DECK"))
        //    {
        //        Console.WriteLine($"Got '{rvmNode.Name}' - {attributes["RefNo"]}");
        //    }
        //    //else
        //    //{
        //    //    return Array.Empty<APrimitive>();
        //    //}
        //}

        PrimitiveAttributes attr = null;
        if (attributes != null)
        {
            if (attributes.Count > 0 && attributes.ContainsKey("Discipline"))
            {
                attr = new PrimitiveAttributes(attributes["Discipline"], attributes["Type"], attributes["RefNo"]);
                //if (!(attr.Discipline == "PIPE" && (attr.Type == "VAUL" || attr.Type == "FLAN")))
                //{
                //    attr = null;
                //}
            }
        }
        IEnumerable<APrimitive> shadowCube = null;
        if (isShadow) // && rvmPrimitive is not RvmEllipticalDish)
        {
            //if (rvmPrimitive is RvmFacetGroup)
            //{
            //    shadowCube = (rvmPrimitive as RvmFacetGroup).ConvertToRevealPrimitive(treeIndex, Color.Yellow, attr);
            //}
            //else
            {
                //if (rvmPrimitive is RvmFacetGroup)
                //{
                //    var actualFacetGroup = (rvmPrimitive as RvmFacetGroup).ConvertToRevealPrimitive(
                //        treeIndex,
                //        Color.Yellow,
                //        attr
                //    );

                //    shadowCube = rvmPrimitive is not null
                //        ? rvmPrimitive.CalculateAsRvmBox(treeIndex, Color.Yellow, attr)
                //        : Array.Empty<APrimitive>();
                //}
                //else
                {
                    if (rvmPrimitive is RvmFacetGroup)
                    {
                        //   Console.WriteLine($"FacetGroup TreeIndex: {treeIndex} RefNo: {attr.refNo}");
                    }
                    shadowCube = rvmPrimitive is not RvmLine
                        //                ? rvmPrimitive.CalculateAsRvmBox(treeIndex, rvmNode.GetColor(), attr)
                        ? rvmPrimitive.CalculateAsRvmBox(treeIndex, rvmNode.GetColor(), attr)
                        : Array.Empty<APrimitive>();
                }
            }
            if (shadowCube.Count() >= 1)
            { // && (rvmPrimitive is not RvmFacetGroup))
                //if (attr.refNo == "=16819/51859") { }
                return shadowCube;
            }
        }
        switch (rvmPrimitive)
        {
            case RvmBox rvmBox:
                var actualBox = rvmBox.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), attr);
                if (shadowCube != null)
                    return shadowCube;
                return actualBox;
            case RvmCylinder rvmCylinder:
                var actualCylinder = rvmCylinder.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), attr);
                //if (shadowCube != null) // Ok-ish?
                //    return shadowCube;
                return actualCylinder;
            case RvmEllipticalDish rvmEllipticalDish:
                var actualEllipticalDish = rvmEllipticalDish.ConvertToRevealPrimitive(
                    treeIndex,
                    rvmNode.GetColor(),
                    attr
                );
                //if (shadowCube != null) // Looks okish?
                //    return shadowCube;
                return actualEllipticalDish;
            case RvmFacetGroup rvmFacetGroup:
                //var actualFacetGroup = rvmFacetGroup.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), attr);
                var actualFacetGroup = rvmFacetGroup.ConvertToRevealPrimitive(treeIndex, Color.Yellow, attr);
                //if (shadowCube != null) // Investigate further
                //    return shadowCube;
                return actualFacetGroup;
            case RvmLine:
                // Intentionally ignored. Can't draw a 2D line in Cognite Reveal.
                return Array.Empty<APrimitive>();
            case RvmPyramid rvmPyramid:
                var actualPyramid = rvmPyramid.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), attr);
                //if (shadowCube != null) //Okish?
                //    return shadowCube;
                return actualPyramid;
            case RvmCircularTorus circularTorus:
                var actualTorus = circularTorus.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), attr);
                //if (shadowCube != null)
                //    return shadowCube;
                return actualTorus;
            case RvmSphere rvmSphere:
                var actualSphere = rvmSphere.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), attr);
                //if (shadowCube != null) //ok-ish?
                //    return shadowCube;
                return actualSphere;
            case RvmSphericalDish rvmSphericalDish:
                var actualSphericalDish = rvmSphericalDish.ConvertToRevealPrimitive(
                    treeIndex,
                    rvmNode.GetColor(),
                    attr
                );
                //if (shadowCube != null) //ok-ish
                //    return shadowCube;
                return actualSphericalDish;
            case RvmSnout rvmSnout:
                var actualSnout = rvmSnout.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), attr);
                //if (shadowCube != null)
                //    return shadowCube;
                return actualSnout;
            case RvmRectangularTorus rvmRectangularTorus:
                var actualRectangularTorus = rvmRectangularTorus.ConvertToRevealPrimitive(
                    treeIndex,
                    rvmNode.GetColor(),
                    attr
                );
                //if (shadowCube != null)
                //    return shadowCube;
                return actualRectangularTorus;
            default:
                throw new ArgumentOutOfRangeException(nameof(rvmPrimitive), rvmPrimitive, nameof(rvmPrimitive));
        }
    }
}
