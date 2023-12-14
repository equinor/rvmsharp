namespace CadRevealRvmProvider.Tessellation;

using CadRevealComposer.Utils;

public class TessellationLogObject
{
    private readonly string _description;
    private readonly Dictionary<int, int> _tessellateFacetGroupFails = new();
    private int _pyramidCounter;

    public TessellationLogObject(string description)
    {
        _description = description;
    }

    public void AddFailedFacetGroup(int numberOfPolygons)
    {
        _tessellateFacetGroupFails.TryGetValue(numberOfPolygons, out int currentValue);
        _tessellateFacetGroupFails[numberOfPolygons] = currentValue + 1;
    }

    public void AddFailedPyramid()
    {
        _pyramidCounter++;
    }

    public void LogFailedTessellations()
    {
        using (new TeamCityLogBlock(_description))
        {
            foreach (var polygonCountGroup in _tessellateFacetGroupFails)
            {
                Console.WriteLine(
                    $"Could not tessellate {polygonCountGroup.Value} facet groups with polygon count: {polygonCountGroup.Key}"
                );
            }

            if (_pyramidCounter > 0)
                Console.WriteLine($"Could not tessellate {_pyramidCounter} pyramids");
        }
    }
}
