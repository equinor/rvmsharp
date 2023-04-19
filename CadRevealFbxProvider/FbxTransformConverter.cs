namespace CadRevealFbxProvider;

using System.Numerics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct FbxTransform
{
    // ReSharper disable FieldCanBeMadeReadOnly.Global -- not needed
    public float posX;
    public float posY;
    public float posZ;
    public float rotX;
    public float rotY;
    public float rotZ;
    public float rotW;
    public float scaleX;
    public float scaleY;
    public float scaleZ;
    // ReSharper restore FieldCanBeMadeReadOnly.Global
}

public static class FbxTransformConverter
{
    
    /// <summary>
    /// Convert a Fbx native Transform to a Matrix4x4
    /// </summary>
    // ReSharper disable once InconsistentNaming -- Matrix4x4 is correct
    public static Matrix4x4 ToMatrix4x4(FbxTransform transform)
    {
        var pos = new Vector3(transform.posX, transform.posY, transform.posZ);
        var rot = new Quaternion(transform.rotX, transform.rotY, transform.rotZ, transform.rotW);
        var sca = new Vector3(transform.scaleX, transform.scaleY, transform.scaleZ);
        return Matrix4x4.CreateScale(sca)
               * Matrix4x4.CreateFromQuaternion(rot)
               * Matrix4x4.CreateTranslation(pos);
    }
}
