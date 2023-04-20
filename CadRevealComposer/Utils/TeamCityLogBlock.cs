namespace CadRevealComposer.Utils;

using System;

public class TeamCityLogBlock : IDisposable
{
    private readonly string? _blockName;
    private bool _isClosed;

    /// <summary>
    /// Helper to create a log block. With special handling for TeamCity
    /// Automatically "opens" a log block on creation.
    /// Use <see cref="Dispose"/> or <see cref="CloseBlock"/> to close the block.
    /// </summary>
    public TeamCityLogBlock(string blockName)
    {
        _blockName = blockName;
        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression -- Makes it less readable
        if (IsOnTeamCity())
        {
            Console.WriteLine($"##teamcity[blockOpened name='{_blockName}']");
        }
        else
        {
            Console.WriteLine($"--- {_blockName} ---");
        }
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
        if (_blockName == null)
            return;

        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression -- Makes it less readable
        if (IsOnTeamCity())
        {
            Console.WriteLine($"##teamcity[blockClosed name='{_blockName}']");
        }
        else
        {
            Console.WriteLine($"--- End {_blockName} ---");
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        CloseBlock();
    }
}
