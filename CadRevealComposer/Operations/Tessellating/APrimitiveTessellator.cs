namespace CadRevealComposer.Operations.Tessellating;

using Primitives;

public static class APrimitiveTessellator
{
    public static TriangleMesh? TryToTessellate(APrimitive primitive)
    {
        switch (primitive)
        {
            case Box box:
                return BoxTessellator.Tessellate(box);
            case EccentricCone cone:
                return EccentricConeTessellator.Tessellate(cone);
            case TorusSegment torus:
                return TorusSegmentTessellator.Tessellate(torus);
            case Cone cone:
                return ConeTessellator.Tessellate(cone);
            case Circle circle:
                return CircleTessellator.Tessellate(circle);
            case GeneralRing generalRing:
                return GeneralRingTessellator.Tessellate(generalRing);

            // TODO Is complex and moved to own user story #131981
            //case EllipsoidSegment ellipsoidSegment:
            //    result.AddRange(EllipsoidSegmentTessellator.Tessellate(ellipsoidSegment));
            //    break;

            // TODO Is complex and moved to own user story #131982
            // case GeneralCylinder cylinder:
            //     result.AddRange(GeneralCylinderTessellator.Tessellate(cylinder));
            //     break;
            default:
                return null;
        }
    }
}
