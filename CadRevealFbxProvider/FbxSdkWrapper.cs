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

    private const string Library = "cfbx";

    public FbxSdkWrapper()
    {
        CreateSdk();
    }

    public void Dispose()
    {
        DestroySdk();
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
