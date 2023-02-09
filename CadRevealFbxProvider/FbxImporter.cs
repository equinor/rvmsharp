namespace CadRevealFbxProvider;

using System;
using CadRevealComposer.Tessellation;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

public class FbxImporter : IDisposable
{
    private const string Library = "cfbx";

    private FbxSdkWrapper sdk;

    public FbxImporter()
    {
        sdk = new FbxSdkWrapper();
    }

    public bool HasValidSdk()
    {
        return sdk.IsValid();
    }

    public FbxNode LoadFile(string filename)
    {
        //this should never be called in the first place if the SDK is invalid
        if(!sdk.IsValid())
        {
            throw new NullReferenceException("Cannot load files with invalid SDK.");
        }
        return sdk.LoadFile(filename);
    }

    public void Dispose()
    {
        sdk.Dispose();
    }
}