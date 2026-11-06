namespace CadRevealComposer.Tests.Writers.GltfWriterUtils;

using CadRevealComposer.Writers.GltfWriterUtils;
using SharpGLTF.Memory;

public class BufferViewAutoOffsetTests
{
    [Test]
    public void AddOffset_IncrementsCurrentOffsetByFormatByteSize()
    {
        // Arrange
        const int unusedCount = 0;
        var bufferViewAutoOffset = new BufferViewAutoOffset(null!, unusedCount);
        var initialOffset = bufferViewAutoOffset.CurrentOffset;
        var format = AttributeFormat.Float3;

        // Act
        bufferViewAutoOffset.MarkFormatUsed(format);

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(initialOffset, Is.Zero);
            Assert.That(bufferViewAutoOffset.CurrentOffset, Is.EqualTo(initialOffset + format.ByteSize));
        }

        var float4X4 = AttributeFormat.Float4x4;
        bufferViewAutoOffset.MarkFormatUsed(float4X4);

        Assert.That(
            bufferViewAutoOffset.CurrentOffset,
            Is.EqualTo(initialOffset + format.ByteSize + float4X4.ByteSize)
        );
    }
}
