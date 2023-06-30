namespace CadRevealComposer;

using Configuration;
using System.Linq;
using System.Text.RegularExpressions;

public class NodeNameFiltering
{
    private readonly Regex[] _nodeNameExcludeGlobs;

    private long CheckedNodes = 0;
    private long ExcludedNodes = 0;

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
        // Naive statkeeping.
        CheckedNodes++;
        var shouldExclude = _nodeNameExcludeGlobs.Any(x => x.IsMatch(nodeName));
        if (shouldExclude)
            ExcludedNodes++;

        return shouldExclude;
    }
}
