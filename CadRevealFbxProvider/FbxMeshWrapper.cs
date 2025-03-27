namespace CadRevealFbxProvider;

using System.Numerics;
using System.Runtime.InteropServices;
using CadRevealComposer.Tessellation;

public static class FbxMeshWrapper
{
    private const string FbxLib = FbxSdkWrapper.FbxLibraryName;

    [StructLayout(LayoutKind.Sequential)]
    private struct FbxMesh
    {
        public bool valid;
        public int vertex_count;
        public int index_count;
        public IntPtr vertex_position_data;
        public IntPtr index_data;
    }

    [DllImport(FbxLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "mesh_clean_memory")]
    private static extern void mesh_clean_memory(IntPtr meshPtr); //IntPtr in is FbxMesh*

    [DllImport(FbxLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "node_get_mesh")]
    private static extern IntPtr node_get_mesh(IntPtr node);

    public static IntPtr GetMeshGeometryPtr(FbxNode node)
    {
        return node_get_mesh(node.NodeAddress);
    }

    // the underlying umanaged code allocates memory, you must call mesh_clean_memory to free it later
    [DllImport(FbxLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "mesh_get_geometry_data")]
    private static extern IntPtr mesh_get_geometry_data(IntPtr mesh); // IntPtr out is FbxMesh*

    public static Mesh? GetGeometricData(IntPtr meshPtr)
    {
        var geomPtr = mesh_get_geometry_data(meshPtr);
        var geom = Marshal.PtrToStructure<FbxMesh>(geomPtr);

        // geometry can be invalid if, e.g., the extraction of normal vectors failed
        if (geom.valid)
        {
            var vCount = geom.vertex_count;
            var iCount = geom.index_count;
            var vertices = new float[vCount * 3];
            var indices = new int[iCount];
            Marshal.Copy(geom.vertex_position_data, vertices, 0, vertices.Length);
            Marshal.Copy(geom.index_data, indices, 0, indices.Length);
            mesh_clean_memory(geomPtr);
            var vv = new Vector3[vCount];

            for (var i = 0; i < vCount; i++)
            {
                vv[i] = new Vector3(vertices[3 * i], vertices[3 * i + 1], vertices[3 * i + 2]);
            }

            var ii = indices.Select(a => (uint)a).ToArray();

            const float error = 0f; // We have no tessellation error info for FBX files.

            // NOTE: We discard normals as they are not used in Reveal. Consider if this is the best way.
            return new Mesh(vv, ii, error);
        }

        mesh_clean_memory(geomPtr);
        return null;
    }
}
