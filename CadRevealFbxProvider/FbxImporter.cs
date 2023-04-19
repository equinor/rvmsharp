namespace CadRevealFbxProvider;

public class FbxImporter : IDisposable
{
    private readonly FbxSdkWrapper _sdk;

    public FbxImporter()
    {
        _sdk = new FbxSdkWrapper();
    }

    public bool HasValidSdk()
    {
        return _sdk.IsValid();
    }

    public FbxNode LoadFile(string filename)
    {
        //this should never be called in the first place if the SDK is invalid
        if (!_sdk.IsValid())
        {
            throw new NullReferenceException("Cannot load files with invalid SDK.");
        }
        return _sdk.LoadFile(filename);
    }

    public void Dispose()
    {
        _sdk.Dispose();
    }
}
