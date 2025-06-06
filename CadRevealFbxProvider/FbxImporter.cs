namespace CadRevealFbxProvider;

public class FbxImporter : IDisposable
{
    private readonly FbxSdkWrapper _sdk = new();

    public bool HasValidSdk()
    {
        return _sdk.IsValid();
    }

    public FbxNode LoadFile(string filename)
    {
        if (!File.Exists(filename))
        {
            throw new FileNotFoundException(filename + " was not found");
        }
        return _sdk.LoadFile(filename);
    }

    public void Dispose()
    {
        _sdk.Dispose();
    }
}
