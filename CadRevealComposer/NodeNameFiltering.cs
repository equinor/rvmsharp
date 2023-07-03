namespace CadRevealComposer;

using Configuration;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Utils;

public class NodeNameFiltering
{
    private readonly Regex[] _nodeNameExcludeGlobs;

    private long _checkedNodes = 0;
    private long _excludedNodes = 0;

    public NodeNameFiltering(NodeNameExcludeGlobs modelParametersNodeNameExcludeGlobs)
    {
        _nodeNameExcludeGlobs = modelParametersNodeNameExcludeGlobs.Values.Select(ConvertGlobToRegex).ToArray();
    }

    private static Regex ConvertGlobToRegex(string glob)
    {
        // Naive glob implementation inspired from https://stackoverflow.com/a/4146349
        return new Regex(
            "^" + Regex.Escape(glob).Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
            RegexOptions.IgnoreCase | RegexOptions.Singleline
        );
    }

    public bool ShouldExcludeNode(string nodeName)
    {
        // Basic stat-keeping.
        _checkedNodes++;

        var shouldExclude = _nodeNameExcludeGlobs.Any(x => x.IsMatch(nodeName));

        if (shouldExclude)
            _excludedNodes++;

        return shouldExclude;
    }

    public void PrintFilteringStatsToConsole()
    {
        using (new TeamCityLogBlock("Filtering Stats"))
        {
            if (!_nodeNameExcludeGlobs.Any())
            {
                Console.WriteLine("Had no Node name filter. No filtering done.");
                return;
            }

            Console.WriteLine(
                "Using these regexes (converted from globs): '"
                    + string.Join("', '", _nodeNameExcludeGlobs.Select(x => x.ToString()))
                    + "'"
            );
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
