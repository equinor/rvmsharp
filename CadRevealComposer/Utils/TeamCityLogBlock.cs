namespace CadRevealComposer.Utils;

using System;

public class TeamCityLogBlock : IDisposable
{
    private readonly string? _blockName;
    private bool _isClosed;

    /// <summary>
    /// Helper to create a log block when running in TeamCity only.
    /// Automatically "opens" a log block on creation.
    /// Use <see cref="Dispose"/> or <see cref="CloseBlock"/> to close the block.
    /// </summary>
    public TeamCityLogBlock(string blockName)
    {
        if (!IsOnTeamCity())
            return;

        _blockName = blockName;
        Console.WriteLine($"##teamcity[blockOpened name='{_blockName}']");
    }

    private static bool IsOnTeamCity()
    {
        string? teamCityVersionEnvVariable = Environment.GetEnvironmentVariable("TEAMCITY_VERSION");
        return !string.IsNullOrEmpty(teamCityVersionEnvVariable);
    }

    /// <summary>
    /// Closes the log block. Alias for <see cref="Dispose"/>.
    /// </summary>
    public void CloseBlock()
    {
        if (_isClosed)
            return;

        _isClosed = true;
        if (_blockName != null)
        {
            Console.WriteLine($"##teamcity[blockClosed name='{_blockName}']");
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        CloseBlock();
    }
}