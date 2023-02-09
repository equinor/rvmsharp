namespace CadRevealFbxProvider;

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class FbxSdkWrapper:IDisposable
{
    private IntPtr sdk;
    private IntPtr version;

    private const string Library = "cfbx";

    private bool isValidSdk = false;

    public FbxSdkWrapper()
    {
        CreateSdk();
        isValidSdk = GetFbxSdkVersion();
    }

    public bool IsValid()
    {
        return isValidSdk;
    }

    public void Dispose()
    {
        ClearFbxSdkVersion(version);
        DestroySdk();
    }

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "get_fbxsdk_version")]
    private static extern IntPtr get_fbxsdk_version();
    /// <summary>
    /// Retrieves the FBX SDK version that was used to build the DLL. Returns true if valid, false if invalid.
    /// </summary>
    public bool GetFbxSdkVersion()
    {
        version = get_fbxsdk_version();
        var versionStr = Marshal.PtrToStringAnsi(version);

        Console.WriteLine("FBX SDK version: " + versionStr);

        if (versionStr == null)
        {
            Console.WriteLine("FBX SDK version is missing");
            return false;
        }
        var versionMinExpected = new Version("2020.3.2");
        var versionObj = new Version(versionStr);
        if(versionObj.CompareTo(versionMinExpected) < 0)
        {
            Console.WriteLine("FBX SDK version is too old. Will be skipping all FBX files.");
            return false;
        }

        return true;
    }

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "delete_fbxsdk_version")]
    private static extern void delete_fbxsdk_version(IntPtr versionString);
    /// <summary>
    /// Clears memory
    /// </summary>
    public void ClearFbxSdkVersion(IntPtr versionString)
    {
        delete_fbxsdk_version(versionString);   
    }


    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "manager_create")]
    private static extern IntPtr manager_create();
    /// <summary>
    /// FBX Importer is NOT thread-safe, and only one instance should be active at a time. Remember to dispose.
    /// </summary>
    public void CreateSdk()
    {
        sdk = manager_create();
    }


    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "manager_destroy")]
    private static extern void manager_destroy(IntPtr manager);
    public void DestroySdk()
    {
        manager_destroy(sdk);
        
        Console.WriteLine("Disposing FBX SDK");
    }

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "load_file")]
    private static extern IntPtr load_file(string filename, IntPtr sdk);
    public FbxNode LoadFile(string filename)
    {
        return new FbxNode(load_file(filename, sdk));
    }

}
