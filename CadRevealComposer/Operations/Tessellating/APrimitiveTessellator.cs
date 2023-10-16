namespace CadRevealComposer.Operations.Tessellating;

using Primitives;
using System.Collections.Generic;

public static class APrimitiveTessellator
{
    public static IEnumerable<APrimitive> TryToTessellate(APrimitive primitive)
    {
        var result = new List<APrimitive>();

        switch (primitive)
        {
            case Box box:
                result.Add(BoxTessellator.Tessellate(box));
                break;
            case EccentricCone cone:
                result.Add(EccentricConeTessellator.Tessellate(cone));
                break;
            case TorusSegment torus:
                result.Add(TorusSegmentTessellator.Tessellate(torus));
                break;
            case Cone cone:
                result.Add(ConeTessellator.Tessellate(cone));
                break;
            case Circle circle:
                result.Add(CircleTessellator.Tessellate(circle));
                break;
            case GeneralRing generalRing:
                result.Add(GeneralRingTessellator.Tessellate(generalRing));
                break;

            // TODO Is complex and moved to own user story #131981
            //case EllipsoidSegment ellipsoidSegment:
            //    result.AddRange(EllipsoidSegmentTessellator.Tessellate(ellipsoidSegment));
            //    break;

            // TODO Is complex and moved to own user story #131982
            // case GeneralCylinder cylinder:
            //     result.AddRange(GeneralCylinderTessellator.Tessellate(cylinder));
            //     break;
            default:
                result.Add(primitive);
                break;
        }

        return result;
    }
}
