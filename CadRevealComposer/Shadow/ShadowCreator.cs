namespace CadRevealComposer.Shadow;

using Primitives;
using System;

public static class ShadowCreator
{
    public static APrimitive CreateShadow(APrimitive primitive)
    {
        switch (primitive)
        {
            case InstancedMesh instancedMesh: // TODO: It is uneccessary to calculate the box for every instance, as the template is the same for each group
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

            // Dummies used while developing
            default:

                throw new Exception("Some primitives were not handled when creating shadows");

            //var dummyScale = new Vector3(0.1f);
            //var dummyRotation = Quaternion.Identity;
            //var dummyPosition = Vector3.Zero;

            //var dummyMatrix =
            //    Matrix4x4.CreateScale(dummyScale)
            //    * Matrix4x4.CreateFromQuaternion(dummyRotation)
            //    * Matrix4x4.CreateTranslation(dummyPosition);

            //var dummyBox = new Box(
            //    dummyMatrix,
            //    primitive.TreeIndex,
            //    primitive.Color,
            //    primitive.AxisAlignedBoundingBox
            //);
            //return dummyBox;
        }
    }
}
