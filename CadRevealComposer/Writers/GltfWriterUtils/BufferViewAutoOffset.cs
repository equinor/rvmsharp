namespace CadRevealComposer.Writers.GltfWriterUtils;

using SharpGLTF.Memory;
using SharpGLTF.Schema2;

/// <summary>
/// Helps to manage the current offset in a buffer view when using interleaved data in a Gltf file.
///
/// Useful only when used with the <see cref="AccessorExtensions.SetDataAutoOffset"/> extension method.
/// </summary>
public class BufferViewAutoOffset(BufferView bufferView, int count)
{
    /// <summary>
    /// The instance of the buffer view.
    /// </summary>
    public BufferView BufferView { get; } = bufferView;

    /// <summary>
    /// The number of elements in the buffer view.
    /// </summary>
    public int Count { get; } = count;

    /// <summary>
    /// The current offset in the buffer view (in bytes) where the next data will be read.
    /// </summary>
    public int CurrentOffset { get; private set; }

    /// <summary>
    /// Adds the format byte size to <see cref="CurrentOffset"/>
    /// </summary>
    public void MarkFormatUsed(AttributeFormat format)
    {
        CurrentOffset += format.ByteSize;
    }
}
