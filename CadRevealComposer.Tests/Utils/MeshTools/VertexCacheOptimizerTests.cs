using NUnit.Framework;

namespace CadRevealComposer.Tests;

using CadRevealComposer.Utils;
using CadRevealComposer.Utils.MeshTools;

[TestFixture]
public class VertexCacheOptimizerTests
{
    [Test]
    public void OptimizeVertexCacheFifo_WithValidInput_GivesValuesFromReferenceImplementation()
    {
        // Expected result is taken directly from running the input through 'zeux/meshoptimizer' v0.18, input data is from a arbitrarily picked mesh on Huldra.
// @formatter:off
        uint[] inputIndices = {0,1,2,3,1,0,6,5,0,0,2,6,6,2,7,0,2,6,7,1,3,3,4,7,8,13,11,9,10,8,10,12,14,11,13,12,14,13,15,15,10,14,9,8,11,11,12,9,16,17,18,18,19,16,20,21,22,22,23,20,20,23,16,16,19,20,18,17,22,22,21,18,18,21,20,20,19,18,16,23,22,22,17,16};
        uint[] expectedFifo = {0,1,2,3,1,0,6,5,0,0,2,6,0,2,6,7,1,3,3,4,7,6,2,7,8,13,11,9,10,8,9,8,11,11,13,12,14,13,15,11,12,9,10,12,14,15,10,14,16,17,18,18,19,16,20,23,16,16,19,20,16,23,22,22,17,16,18,17,22,22,21,18,18,21,20,20,19,18,20,21,22,22,23,20};
// @formatter:on

        uint[] output = new uint[inputIndices.Length];
        VertexCacheOptimizer.OptimizeVertexCacheFifo(output, inputIndices, 24, 16);
        Assert.That(output, Is.Not.EquivalentTo(inputIndices));
        Assert.That(expectedFifo, Is.EquivalentTo(output));
    }

    [Test]
    public void OptimizeVertexCache_WithValidInput_GivesValuesFromReferenceImplementation()
    {
        // Expected result is taken directly from running the input through 'zeux/meshoptimizer' v0.18, input data is from a arbitrarily picked mesh on Huldra.
// @formatter:off
        uint[] inputIndices = {0,1,2,3,1,0,6,5,0,0,2,6,6,2,7,0,2,6,7,1,3,3,4,7,8,13,11,9,10,8,10,12,14,11,13,12,14,13,15,15,10,14,9,8,11,11,12,9,16,17,18,18,19,16,20,21,22,22,23,20,20,23,16,16,19,20,18,17,22,22,21,18,18,21,20,20,19,18,16,23,22,22,17,16};
        uint[] expected     = {0,1,2,3,1,0,7,1,3,3,4,7,6,2,7,0,2,6,0,2,6,6,5,0,8,13,11,11,13,12,9,8,11,11,12,9,9,10,8,10,12,14,14,13,15,15,10,14,16,17,18,18,17,22,22,17,16,18,19,16,16,23,22,22,21,18,20,23,16,22,23,20,16,19,20,20,21,22,20,19,18,18,21,20};
// @formatter:on
        uint[] output = new uint[inputIndices.Length];
        VertexCacheOptimizer.OptimizeVertexCache(output, inputIndices, 24);
        Assert.That(output, Is.Not.EquivalentTo(inputIndices));
        Assert.That(output, Is.EquivalentTo(expected));
    }
}