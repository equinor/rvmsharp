namespace CadRevealComposer.Operations;

using Configuration;
using System.Text.RegularExpressions;

public enum NodePriority
{
    High,
    Medium,
    Default
}

public class PriorityMapping
{
    private readonly Regex? _disciplineRegex; //
    private readonly Regex? _nodeNameRegex;

    public PriorityMapping(PrioritizedDisciplinesRegex disciplineRegex, PrioritizedNodeNamesRegex nodeNameRegex)
    {
        if (disciplineRegex.Value != null)
        {
            _disciplineRegex = new Regex(disciplineRegex.Value, RegexOptions.IgnoreCase);
        }

        if (nodeNameRegex.Value != null)
        {
            _nodeNameRegex = new Regex(nodeNameRegex.Value, RegexOptions.IgnoreCase);
        }
    }

    public NodePriority GetPriority(string discipline, string nodeName)
    {
        if (_nodeNameRegex != null && _nodeNameRegex.IsMatch(nodeName))
            return NodePriority.High;

        if (_disciplineRegex != null && _disciplineRegex.IsMatch(discipline))
            return NodePriority.Medium;

        return NodePriority.Default;
    }
}
