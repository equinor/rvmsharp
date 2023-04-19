namespace RvmSharp;

using Commons;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

public static class PdmsTextParser
{
    public record PdmsNode(
        string Name,
        Dictionary<string, string> MetadataDict,
        PdmsNode? Parent,
        List<PdmsNode> Children);

    private class StatefulReader : StreamReader
    {
        public int LineNumber { get; private set; }

        public StatefulReader(string filename) : base(filename) {}

        override public string? ReadLine()
        {
            var line = base.ReadLine();
            LineNumber++;
            return line;
        }
    }

    public static List<PdmsNode> GetAllPdmsNodesInFile(string pdmsTxtFilePath)
    {
        return GetAllPdmsNodesInFile(pdmsTxtFilePath, Array.Empty<string>(), null);
    }

    /// <summary>
    /// Performance oriented version of GetAllPdmsNodesInFile which may reduce memory allocations, memory usage and CPU processing time.
    /// </summary>
    /// <param name="pdmsTxtFilePath">File path RVM TEXT</param>
    /// <param name="attributesToExclude">Exclude node attributes by name (case sensitive). If a attribute is not needed this can help to avoid string memory allocations and reduce processing time.</param>
    /// <param name="stringInternPool">String intern pool to deduplicate string allocations and reuse string instances.</param>
    public static List<PdmsNode> GetAllPdmsNodesInFile(string pdmsTxtFilePath, IReadOnlyList<string> attributesToExclude, IStringInternPool? stringInternPool)
    {

        var pdmsNodes = new List<PdmsNode>();

        using (var reader = new StatefulReader(pdmsTxtFilePath))
        {
            var indentationStack = new Stack<int>();
            PdmsNode? currentPdmsNode = null;

            var headerInfo = ParseHeader(reader);
            var newItemSeparator = headerInfo.StartSeparator.AsSpan();
            int newItemSeparatorLength = newItemSeparator.Length;
            var endItemSeparator = headerInfo.EndSeparator.AsSpan();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (line == null)
                    throw new NullReferenceException( $"Unexpected null in {nameof(line)}");

                var lineSpan = line.AsSpan();

                var trimmedLine = lineSpan.Trim();
                if (trimmedLine.StartsWith(newItemSeparator, StringComparison.Ordinal))
                {
                    var pdmsNode = new PdmsNode
                    (
                        Name: trimmedLine[newItemSeparatorLength..].Trim().ToString(),
                        MetadataDict: new Dictionary<string, string>(),
                        Parent: currentPdmsNode,
                        Children: new List<PdmsNode>()
                    );

                    indentationStack.Push(lineSpan[..lineSpan.IndexOf(newItemSeparator[0])].Length);

                    if (currentPdmsNode == null)
                        pdmsNodes.Add(pdmsNode);
                    else
                        currentPdmsNode.Children.Add(pdmsNode);

                    currentPdmsNode = pdmsNode;
                }
                else
                {
                    if (trimmedLine.Equals(endItemSeparator, StringComparison.Ordinal))
                    {
                        var indentation = lineSpan[..lineSpan.IndexOf(endItemSeparator[0])].Length;
                        if (indentation != indentationStack.Peek())
                        {
                            Console.Error.WriteLine($"Invalid END at line number: {reader.LineNumber}");
                        }
                        else
                        {
                            indentationStack.Pop();
                            currentPdmsNode = currentPdmsNode!.Parent;
                        }
                    }
                    else
                    {
                        var nameSeparatorIndex = trimmedLine.IndexOf(headerInfo.NameEnd, StringComparison.InvariantCulture);
                        var key = GetKey(trimmedLine, nameSeparatorIndex);

                        if (!IsExcludedAttribute(key, attributesToExclude))
                        {
                            var value = GetValue(trimmedLine, nameSeparatorIndex + headerInfo.NameEnd.Length);
                            if (stringInternPool  != null)
                            {
                                var keyInterned = stringInternPool.Intern(key);
                                var valueInterned = stringInternPool.Intern(StripQuotes(value));
                                currentPdmsNode!.MetadataDict[keyInterned] = valueInterned;
                            } else
                            {
                                currentPdmsNode!.MetadataDict[key.ToString()] = StripQuotes(value).ToString();
                            }

                        }
                    }
                }
            }
        }

        return pdmsNodes;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsExcludedAttribute(ReadOnlySpan<char> attributeName, IReadOnlyList<string> attributesToExclude)
    {
        for (var i = 0; i < attributesToExclude.Count; i++)
        {
            if (attributeName.SequenceEqual(attributesToExclude[i]))
            {
                return true;
            }
        }

        return false;
    }

    private class PdmsFileHeader
    {
        public string FileFormat = "Unknown";
        public string StartSeparator = "NEW";
        public string EndSeparator = "END";
        public string NameEnd = ":=";
        public string Sep = "&end&";
        // ReSharper disable once CollectionNeverQueried.Local -- We currently do not use this for anything, but it can be used in the future.
        public readonly Dictionary<string, string> HeaderMetadata = new();
    }

    private static PdmsFileHeader ParseHeader(StreamReader reader)
    {
        var header = new PdmsFileHeader();

        string[] cadcAttributesFilesSupported = {"CADC_Attributes_File v1.0"};

        // Parse the first line:
        string? firstLine = reader.ReadLine();
        if (firstLine == null)
        {
            throw new Exception("PDMS file header not found!");
        }

        var firstLineSegments = firstLine.Split(',').Select(x => x.Trim()).ToArray();
        header.FileFormat = firstLineSegments[0];
        if (!cadcAttributesFilesSupported.Contains(header.FileFormat))
        {
            throw new Exception(
                $"Unsupported PDMS file header: {header.FileFormat}. Expected one of: {string.Join(", ", cadcAttributesFilesSupported)}");
        }
        header.StartSeparator = firstLineSegments[1].Split(':')[1].Trim();
        header.EndSeparator = firstLineSegments[2].Split(':')[1].Trim();
        header.NameEnd = firstLineSegments[3].Substring("name_end:".Length).Trim();
        header.Sep = firstLineSegments[4].Split(':')[1].Trim();

        // Read Header Information:
        var firstLineHeaderInformation = reader.ReadLine()!;
        if (firstLineHeaderInformation.StartsWith($"{header.StartSeparator} Header Information"))
        {
            bool headerEnded = false;
            while (!headerEnded)
            {
                string currentLine = reader.ReadLine()!;
                if (currentLine.EndsWith(header.EndSeparator))
                {
                    headerEnded = true;
                }
                else
                {
                    var lineSegments = currentLine.Split(new[] {header.Sep}, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string keyValueSegment in lineSegments)
                    {
                        var nameSeparatorIndex = keyValueSegment.IndexOf(header.NameEnd, StringComparison.InvariantCulture);
                        var key = GetKey(keyValueSegment, nameSeparatorIndex).ToString();
                        var value = GetValue(keyValueSegment, nameSeparatorIndex + header.NameEnd.Length).ToString();
                        header.HeaderMetadata[key] = value;
                    }
                }
            }
        }

        return header;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<char> GetKey(ReadOnlySpan<char> keyValueSegment, int nameSeparatorIndex)
    {
        return keyValueSegment[..nameSeparatorIndex].Trim();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<char> GetValue(ReadOnlySpan<char> keyValueSegment, int nameSeparatorIndex)
    {
        return keyValueSegment[(nameSeparatorIndex)..].Trim();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<char> StripQuotes(ReadOnlySpan<char> input)
    {
        if (input.IsWhiteSpace() || input.Length < 2)
            return input;

        if (input[0] == input[^1] && (input[0] == '\'' || input[0] == '"'))
            return input.Slice(1, input.Length - 2);

        return input;
    }
}