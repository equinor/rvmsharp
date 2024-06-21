namespace CadRevealComposer.Operations;

using System;
using System.Text.RegularExpressions;
using Configuration;
using Utils;

public class NodeNameFiltering
{
    private readonly Regex? _nodeNameExcludeGlobs;

    private long _checkedNodes = 0;
    private long _excludedNodes = 0;

    public NodeNameFiltering(NodeNameExcludeRegex modelParametersNodeNameExcludeGlobs)
    {
        if (modelParametersNodeNameExcludeGlobs.Value != null)
        {
            _nodeNameExcludeGlobs = new Regex(modelParametersNodeNameExcludeGlobs.Value, RegexOptions.IgnoreCase);
        }
    }

    public bool ShouldExcludeNode(string nodeName)
    {
        // Basic stat-keeping.
        _checkedNodes++;

        var shouldExclude = _nodeNameExcludeGlobs?.IsMatch(nodeName) == true;

        if (shouldExclude)
            _excludedNodes++;

        return shouldExclude;
    }

    public void PrintFilteringStatsToConsole()
    {
        using (new TeamCityLogBlock("Filtering Stats"))
        {
            if (_nodeNameExcludeGlobs == null)
            {
                Console.WriteLine("Had no Node name filter. No filtering done.");
                return;
            }

            Console.WriteLine("Using this regex: " + _nodeNameExcludeGlobs);
            Console.WriteLine(
                "Checked "
                    + _checkedNodes
                    + " nodes and filtered out "
                    + _excludedNodes
                    + $". That is {_excludedNodes / (float)_checkedNodes:P1} nodes removed."
                    // Technically it should be fast-ish to check all child counts of removed nodes but no use-case for
                    // it yet. Just adding a remark here to avoid questions
                    + "\nRemark: We dont check any children of excluded nodes so the amount of excluded nodes is unknown"
            );
        }
    }
}
