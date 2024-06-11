namespace CadRevealFbxProvider;

using System.Runtime.InteropServices;

public class FbxSdkWrapper : IDisposable
{
    public const string FbxLibraryName = "cfbx";
    private IntPtr _sdk;

    private readonly bool _isValidSdk;

    public FbxSdkWrapper()
    {
        CreateSdk();
        _isValidSdk = assert_fbxsdk_version("2020.3.2");
    }

    public bool IsValid()
    {
        return _isValidSdk;
    }

    public void Dispose()
    {
        DestroySdk();
    }

    [DllImport(FbxLibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "assert_fbxsdk_version")]
    private static extern bool assert_fbxsdk_version(string versionString);

    [DllImport(FbxLibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "manager_create")]
    private static extern IntPtr manager_create();

    /// <summary>
    /// FBX Importer is NOT thread-safe, and only one instance should be active at a time. Remember to dispose.
    /// </summary>
    private void CreateSdk()
    {
        _sdk = manager_create();
    }

    [DllImport(FbxLibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "manager_destroy")]
    private static extern void manager_destroy(IntPtr manager);

    private void DestroySdk()
    {
        manager_destroy(_sdk);

        Console.WriteLine("Disposing FBX SDK");
    }

    [DllImport(FbxLibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "load_file")]
    private static extern IntPtr load_file(string filename, IntPtr sdk);

    public FbxNode LoadFile(string filename)
    {
        return new FbxNode(load_file(filename, _sdk));
    }
}
