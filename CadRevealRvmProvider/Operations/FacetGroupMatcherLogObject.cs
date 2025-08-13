namespace CadRevealRvmProvider.Operations;

using CadRevealComposer.Utils;

public class FacetGroupMatcherLogObject
{
    private readonly List<FacetGroupMatchingResult> _facetGroupMatchingResults = new();

    public void AddFacetGroupMatchingResult(
        long instancedCount,
        int numberOfFacetGroups,
        long templateCount,
        int vertexCount,
        long iterations,
        double timeElapsed
    )
    {
        _facetGroupMatchingResults.Add(
            new FacetGroupMatchingResult()
            {
                InstanceCount = instancedCount,
                NumberOfFacetGroups = numberOfFacetGroups,
                TemplateCount = templateCount,
                VertexCount = vertexCount,
                Iterations = iterations,
                TimeElapsed = timeElapsed,
            }
        );
    }

    private class FacetGroupMatchingResult
    {
        public long InstanceCount;
        public int NumberOfFacetGroups;
        public long TemplateCount;
        public int VertexCount;
        public long Iterations;
        public double TimeElapsed;
    }

    public void LogFacetGroupMatchingResults()
    {
        using (new TeamCityLogBlock("Facet group matching results"))
        {
            foreach (var result in _facetGroupMatchingResults)
            {
                var fraction = result.InstanceCount / (float)result.NumberOfFacetGroups;
                Console.WriteLine(
                    $"\tFound {result.InstanceCount, 9:N0} instances in {result.InstanceCount, 7:N0} items ({fraction, 7:P1})."
                        + $" TC: {result.TemplateCount, 5:N0}, VC: {result.VertexCount, 6:N0}, IC: {result.Iterations, 10:N0} in {result.TimeElapsed, 6:N}s."
                );
            }
        }
    }
}
