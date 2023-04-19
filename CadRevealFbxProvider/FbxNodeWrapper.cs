namespace CadRevealFbxProvider;

using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

public record struct FbxNode(IntPtr NodeAddress);

public static class FbxNodeWrapper
{
    private const string Library = FbxSdkWrapper.FbxLibraryName;

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "node_get_name")]
    private static extern void node_get_name(IntPtr node, StringBuilder nameOut, int bufferSize);

    public static string GetNodeName(FbxNode node)
    {
        StringBuilder sb = new StringBuilder(512);
        node_get_name(node.NodeAddress, sb, 512);
        return sb.ToString();
    }

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "node_get_child_count")]
    private static extern int node_get_child_count(IntPtr node);

    public static int GetChildCount(FbxNode node)
    {
        return node_get_child_count(node.NodeAddress);
    }

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "node_get_child")]
    private static extern IntPtr node_get_child(IntPtr node, int index);

    public static FbxNode GetChild(int index, FbxNode node)
    {
        return new FbxNode(node_get_child(node.NodeAddress, index));
    }

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "node_get_transform")]
    private static extern FbxTransform node_get_transform(IntPtr node);

    public static Matrix4x4 GetTransform(FbxNode node)
    {
        var transform = node_get_transform(node.NodeAddress);
        return FbxTransformConverter.ToMatrix4x4(transform);
    }
}
