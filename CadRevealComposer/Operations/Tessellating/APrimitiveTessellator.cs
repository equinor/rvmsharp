namespace CadRevealComposer.Operations.Tessellating;

using Primitives;
using System.Collections.Generic;
using System.Drawing;

public static class APrimitiveTessellator
{
    public static IEnumerable<APrimitive> TryToTessellate(APrimitive primitive)
    {
        var result = new List<APrimitive>();

        // TODO: Circle, Ring, EllipsoidSegment,


        switch (primitive)
        {
            //case Box box:
            //    result.AddRange(BoxTessellator.Tessellate(box));
            //    break;
            //case EccentricCone cone:
            //    result.AddRange(EccentricConeTessellator.Tessellate(cone));
            //    break;
            //case TorusSegment torus:
            //    result.AddRange(TorusSegmentTessellator.Tessellate(torus));
            //    break;
            //case Cone cone:
            //    result.AddRange(ConeTessellator.Tessellate(cone));
            //    break;
            case Circle circle:
                result.AddRange(CircleTessellator.Tessellate(circle));
                break;

            //TODO Doesn't work properly, yet...
            // case GeneralCylinder cylinder:
            //     result.AddRange(GeneralCylinderTessellator.Tessellate(cylinder));
            //     break;
            default:
                result.Add(primitive with { Color = Color.Gray });
                break;
        }

        return result;
    }
}
