namespace CadRevealFbxProvider;

using CadRevealComposer.Tessellation;
using System.Numerics;
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
    private static extern FbxTransform node_get_transform(IntPtr node);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "node_get_mesh")]
    private static extern IntPtr node_get_mesh(IntPtr node);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "mesh_clean")]
    private static extern void mesh_clean(FbxMesh mesh_data);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "mesh_get_geometry_data")]
    private static extern FbxMesh mesh_get_geometry_data(IntPtr mesh);

    private IntPtr sdk;

    [StructLayout(LayoutKind.Sequential)]
    public struct FbxTransform
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
    private struct FbxMesh
    {
        public int vertex_count;
        public int triangle_count;
        public IntPtr vertex_data;
        public IntPtr normal_data;
        public IntPtr triangle_data;
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
        var transform = node_get_transform(node.NodeAddress);
        return transform;
    }

    public IntPtr GetMeshGeometryPtr(FbxNode node)
    {
        return node_get_mesh(node.NodeAddress);
    }

    public (Mesh Mesh, IntPtr MeshPtr)? GetGeometricData(FbxNode node)
    {
        var meshPtr = node_get_mesh(node.NodeAddress);
        if (meshPtr != IntPtr.Zero)
        {
            var geom = mesh_get_geometry_data(meshPtr);
            // Console.WriteLine("Number of vertices: " + geom.vertex_count);
            var vCount = geom.vertex_count;
            var vertices = new float[vCount * 3];
            var normals = new float[vCount * 3];
            var indicies = new int[geom.triangle_count];
            Marshal.Copy(geom.vertex_data, vertices, 0, vertices.Length);
            Marshal.Copy(geom.normal_data, normals, 0, normals.Length);
            Marshal.Copy(geom.triangle_data, indicies, 0, indicies.Length);
            mesh_clean(geom);
            var vv = new Vector3[vCount];
            var nn = new Vector3[vCount];
            for (var i = 0; i < vCount; i++)
            {
                vv[i] = new Vector3(vertices[3 * i], vertices[3 * i + 1], vertices[3 * i + 2]);
                nn[i] = new Vector3(normals[3 * i], normals[3 * i + 1], normals[3 * i + 2]);
            }

            var ii = indicies.Select(a => (uint)a).ToArray();

            const float error = 0f; // We have no tessellation error info for FBX files.
            Mesh meshData = new Mesh(vv, nn, ii, error);
            return (meshData, meshPtr);
        }

        return null;
    }

    public void Dispose()
    {
        manager_destroy(sdk);
        Console.WriteLine("Disposing FBX SDK");
    }
}