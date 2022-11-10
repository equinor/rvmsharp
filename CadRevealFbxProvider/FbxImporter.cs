namespace CadRevealFbxProvider;

using System.Runtime.InteropServices;
using System.Text;

public class FbxImporter : IDisposable
{
    private const string Library = "cfbx";

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "manager_create")]
    private static extern IntPtr manager_create();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "manager_destroy")]
    private static extern void manager_destroy(IntPtr manager);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "load_file")]
    private static extern IntPtr load_file(string filename, IntPtr sdk);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "node_get_child_count")]
    private static extern int node_get_child_count(IntPtr node);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "node_get_child")]
    private static extern IntPtr node_get_child(IntPtr node, int index);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "node_get_name")]
    private static extern void node_get_name(IntPtr node, StringBuilder nameOut, int bufferSize);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "node_get_transform")]
    private static extern void node_get_transform(IntPtr node, [In, Out] FbxTransform transform);

    private IntPtr sdk;

    [StructLayout(LayoutKind.Sequential)]
    public class FbxTransform
    {
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
    }

    [StructLayout(LayoutKind.Sequential)]
    private class FbxMesh
    {
        int vertex_count;
        int triangle_count;
        IntPtr vertex_data;
        IntPtr normal_data;
        IntPtr triangle_data;
    }

    public record struct FbxNode(IntPtr NodeAddress);

    public FbxImporter()
    {
        sdk = manager_create();
    }

    public FbxNode LoadFile(string filename)
    {
        return new FbxNode(load_file(filename, sdk));
    }

    public string GetNodeName(FbxNode node)
    {
        StringBuilder sb = new StringBuilder(512);
        node_get_name(node.NodeAddress, sb, 512);
        return sb.ToString();
    }

    public int GetChildCount(FbxNode node)
    {
        return node_get_child_count(node.NodeAddress);
    }

    public FbxNode GetChild(int index, FbxNode node)
    {
        return new FbxNode(node_get_child(node.NodeAddress, index));
    }

    public FbxTransform GetTransform(FbxNode node)
    {
        var transform = new FbxTransform();
        node_get_transform(node.NodeAddress, transform);
        return transform;
    }

    public void Dispose()
    {
        manager_destroy(sdk);
        Console.WriteLine("Disposing FBX SDK");
    }
}