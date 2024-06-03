namespace CadRevealFbxProvider;

using System.Runtime.InteropServices;

public class FbxSdkWrapper : IDisposable
{
    public const string FbxLibraryName = "cfbx";
    private IntPtr _sdk;
    private IntPtr _version;

    private readonly bool _isValidSdk;

    public FbxSdkWrapper()
    {
        CreateSdk();
        _isValidSdk = AssertFbxSdkVersion();
    }

    public bool IsValid()
    {
        return _isValidSdk;
    }

    public void Dispose()
    {
        DestroySdk();
    }

    [DllImport(FbxLibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "get_fbxsdk_version")]
    private static extern IntPtr get_fbxsdk_version();

    [DllImport(FbxLibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "delete_fbxsdk_version")]
    private static extern void delete_fbxsdk_version(IntPtr versionString);

    /// <summary>
    /// Retrieves the FBX SDK version that was used to build the DLL. Returns true if valid, false if invalid.
    /// Clears the memory afterwards (unmanaged code)
    /// </summary>
    public bool AssertFbxSdkVersion()
    {
        _version = get_fbxsdk_version();
        var versionStr = Marshal.PtrToStringAnsi(_version);

        Console.WriteLine("Using FBX SDK version: " + versionStr);

        if (versionStr == null)
        {
            Console.WriteLine("FBX SDK version is missing.");
            return false;
        }

        var trimmedVersion = versionStr.TrimStart('\u0010');;

        var versionMinExpected = new Version("2020.3.2");
        var versionObj = new Version(trimmedVersion);
        if (versionObj.CompareTo(versionMinExpected) < 0)
        {
            Console.WriteLine("FBX SDK version is too old. Cannot load this FBX file.");
            return false;
        }

        delete_fbxsdk_version(_version);
        return true;
    }

    [DllImport(FbxLibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "manager_create")]
    private static extern IntPtr manager_create();

    /// <summary>
    /// FBX Importer is NOT thread-safe, and only one instance should be active at a time. Remember to dispose.
    /// </summary>
    public void CreateSdk()
    {
        _sdk = manager_create();
    }

    [DllImport(FbxLibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "manager_destroy")]
    private static extern void manager_destroy(IntPtr manager);

    public void DestroySdk()
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
