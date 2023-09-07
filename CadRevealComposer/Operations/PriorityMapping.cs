namespace CadRevealComposer.Operations;

using Configuration;
using System.Text.RegularExpressions;

public enum NodePriority
{
    High,
    Medium,
    Low, // Less than default
    Default
}

public class PriorityMapping
{
    private readonly Regex? _disciplineRegex;
    private readonly Regex? _lowDisciplineRegex;
    private readonly Regex? _nodeNameRegex;

    public PriorityMapping(
        PrioritizedDisciplinesRegex disciplineRegex,
        LowPrioritizedDisciplineRegex lowDisciplineRegex,
        PrioritizedNodeNamesRegex nodeNameRegex
    )
    {
        if (disciplineRegex.Value != null)
        {
            _disciplineRegex = new Regex(disciplineRegex.Value, RegexOptions.IgnoreCase);
        }

        if (lowDisciplineRegex.Value != null)
        {
            _lowDisciplineRegex = new Regex(lowDisciplineRegex.Value, RegexOptions.IgnoreCase);
        }

        if (nodeNameRegex.Value != null)
        {
            _nodeNameRegex = new Regex(nodeNameRegex.Value, RegexOptions.IgnoreCase);
        }
    }

    public NodePriority GetPriority(string discipline)
    {
        // TODO
        // if (_nodeNameRegex != null && _nodeNameRegex.IsMatch(nodeName))
        //     return NodePriority.High;

        if (_disciplineRegex != null && _disciplineRegex.IsMatch(discipline))
            return NodePriority.Medium;

        if (_lowDisciplineRegex != null && _lowDisciplineRegex.IsMatch(discipline))
            return NodePriority.Low;

        return NodePriority.Default;
    }
}
