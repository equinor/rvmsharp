namespace CadRevealFbxProvider;

using System.Diagnostics;
using System.Runtime.InteropServices;

public class FbxSdkWrapper : IDisposable
{
    public const string FbxLibraryName = "cfbx";
    private IntPtr sdk;
    private IntPtr version;

    private bool isValidSdk = false;

    public FbxSdkWrapper()
    {
        CreateSdk();
        isValidSdk = AssertFbxSdkVersion();
    }

    public bool IsValid()
    {
        return isValidSdk;
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
        version = get_fbxsdk_version();
        var versionStr = Marshal.PtrToStringAnsi(version);

        Console.WriteLine("Using FBX SDK version: " + versionStr);

        if (versionStr == null)
        {
            Console.WriteLine("FBX SDK version is missing.");
            return false;
        }

        var versionMinExpected = new Version("2020.3.2");
        var versionObj = new Version(versionStr);
        if (versionObj.CompareTo(versionMinExpected) < 0)
        {
            Console.WriteLine("FBX SDK version is too old. Cannot load this FBX file.");
            return false;
        }

        delete_fbxsdk_version(version);
        return true;
    }

    [DllImport(FbxLibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "manager_create")]
    private static extern IntPtr manager_create();

    /// <summary>
    /// FBX Importer is NOT thread-safe, and only one instance should be active at a time. Remember to dispose.
    /// </summary>
    public void CreateSdk()
    {
        sdk = manager_create();
    }

    [DllImport(FbxLibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "manager_destroy")]
    private static extern void manager_destroy(IntPtr manager);

    public void DestroySdk()
    {
        var destroyTimer = Stopwatch.StartNew();
        Console.WriteLine("Disposing FBX SDK...");
        manager_destroy(sdk);
        // For some reason this may be very slow on some files (hours...) Adding log, so it's easy to see what's happening.
        Console.WriteLine("Disposed FBX Sdk in " + destroyTimer.Elapsed);
    }

    [DllImport(FbxLibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "load_file")]
    private static extern IntPtr load_file(string filename, IntPtr sdk);

    public FbxNode LoadFile(string filename)
    {
        return new FbxNode(load_file(filename, sdk));
    }
}
