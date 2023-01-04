namespace CadRevealFbxProvider;

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

    public FbxNode LoadFile(string filename)
    {
        return sdk.LoadFile(filename);
    }

    public void Dispose()
    {
        sdk.Dispose();
    }


    
}