namespace CadRevealFbxProvider;

using CadRevealFbxProvider.UserFriendlyLogger;

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
            throw new UserFriendlyLogException(
                $"Cannot process file {filename}, as it is no longer found. The reason might be some server issues. Please notify the Echo developing team.",
                new FileNotFoundException(filename + " was not found")
            );
        }
        return _sdk.LoadFile(filename);
    }

    public void Dispose()
    {
        _sdk.Dispose();
    }
}
