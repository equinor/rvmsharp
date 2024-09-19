namespace CadRevealFbxProvider;

using System.Drawing;
using System.Runtime.InteropServices;

internal static class FbxMaterialWrapper
{
    private const string FbxLib = FbxSdkWrapper.FbxLibraryName;

    [StructLayout(LayoutKind.Sequential)]
    private struct FbxColor
    {
        public float r;
        public float g;
        public float b;
        public float a;
    }

    [DllImport(FbxLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "material_clean_memory")]
    private static extern void material_clean_memory(IntPtr colorPtr); //IntPtr in is FbxSurfaceLambert*

    [DllImport(FbxLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "node_get_material")]
    private static extern IntPtr node_get_material(IntPtr node);

    // the underlying unmanaged code allocates memory, you must call material_destroy to free it later
    [DllImport(FbxLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "material_get_color")]
    private static extern IntPtr material_get_color(IntPtr material);

    public static Color GetMaterialColor(FbxNode node)
    {
        var materialPtr = node_get_material(node.NodeAddress);
        if (materialPtr == IntPtr.Zero)
        {
            return Color.Yellow;
        }

        var colorPtr = material_get_color(materialPtr);
        var color = Marshal.PtrToStructure<FbxColor>(colorPtr);
        material_clean_memory(colorPtr);

        return Color.FromArgb(ToInt(color.a), ToInt(color.r), ToInt(color.g), ToInt(color.b));

        int ToInt(float value) => (int)(value * 255);
    }
}
