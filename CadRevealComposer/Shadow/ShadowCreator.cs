namespace CadRevealComposer.Shadow;

using Primitives;
using System.Numerics;

public static class ShadowCreator
{
    public static APrimitive CreateShadow(APrimitive primitive)
    {
        switch (primitive)
        {
            case GeneralCylinder cylinder:
                return cylinder.CreateShadow();
            case Cone cone:
                return cone.CreateShadow();
            default:

                var dummyScale = new Vector3(0.1f);
                var dummyRotation = Quaternion.Identity;
                var dummyPosition = Vector3.Zero;

                var dummyMatrix =
                    Matrix4x4.CreateScale(dummyScale)
                    * Matrix4x4.CreateFromQuaternion(dummyRotation)
                    * Matrix4x4.CreateTranslation(dummyPosition);

                var dummyBox = new Box(
                    dummyMatrix,
                    primitive.TreeIndex,
                    primitive.Color,
                    primitive.AxisAlignedBoundingBox
                );
                return dummyBox;
        }
    }
}
