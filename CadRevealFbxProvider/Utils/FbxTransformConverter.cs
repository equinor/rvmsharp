namespace CadRevealFbxProvider.Utils;

using System.Numerics;

public static class FbxTransformConverter
{

    // ReSharper disable once InconsistentNaming -- Matrix4x4 is correct
    public static Matrix4x4 ToMatrix4x4(FbxImporter.FbxTransform transform)
    {
        var pos = new Vector3(transform.posX, transform.posY, transform.posZ);
        var rot = new Quaternion(transform.rotX, transform.rotY, transform.rotZ, transform.rotW);
        var sca = new Vector3(transform.scaleX, transform.scaleY, transform.scaleZ);
        return Matrix4x4.CreateScale(sca)
               * Matrix4x4.CreateFromQuaternion(rot)
               * Matrix4x4.CreateTranslation(pos);
    }
}
