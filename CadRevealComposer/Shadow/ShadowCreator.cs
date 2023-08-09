namespace CadRevealComposer.Shadow;

using Primitives;
using System;

public static class ShadowCreator
{
    public static APrimitive CreateShadow(APrimitive primitive)
    {
        switch (primitive)
        {
            case InstancedMesh instancedMesh: // TODO: It is probably uneccessary to calculate the box for every instance, as the template is the same for each group
                return instancedMesh.CreateShadow();
            case TriangleMesh triangleMesh:
                return triangleMesh.CreateShadow();
            case Box box: // Boxes can stay as they are
                return box;
            case GeneralCylinder cylinder:
                return cylinder.CreateShadow();
            case Cone cone:
                return cone.CreateShadow();
            case EccentricCone eccentricCone:
                return eccentricCone.CreateShadow();
            case EllipsoidSegment ellipsoidSegment:
                return ellipsoidSegment.CreateShadow();
            case Nut nut: // Nut is currently not used
                return nut;
            case TorusSegment torusSegment:
                return torusSegment.CreateShadow();

            default:
                throw new Exception("Some primitives were not handled when creating shadows");
        }
    }
}
