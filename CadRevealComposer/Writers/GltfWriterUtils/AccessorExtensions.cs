namespace CadRevealComposer.Writers.GltfWriterUtils;

using SharpGLTF.Memory;
using SharpGLTF.Schema2;

public static class AccessorExtensions
{
    /// <summary>
    /// Uses the buffer view helper to set data at the current offset, and stores the new offset back in the helper.
    /// Use this method to correctly assign interleaved data to an accessor.
    ///
    /// REMARK: It auto-increments the offset in the buffer view helper, so that sequential calls will write sequential data!
    /// </summary>
    /// <param name="accessor"></param>
    /// <param name="bufferViewAutoOffset">"Keeps" the state of the current offset</param>
    /// <param name="format">Format of the data</param>
    public static void SetDataAutoOffset(
        this Accessor accessor,
        BufferViewAutoOffset bufferViewAutoOffset,
        AttributeFormat format
    )
    {
        var offset = bufferViewAutoOffset.CurrentOffset;
        accessor.SetData(bufferViewAutoOffset.BufferView, offset, bufferViewAutoOffset.Count, format);
        bufferViewAutoOffset.MarkFormatUsed(format);
    }
}
