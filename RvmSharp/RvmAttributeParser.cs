namespace RvmSharp
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public static class PdmsTextParser
    {
        private static readonly char[] Quotes = { '\'', '"' };

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
            List<PdmsNode> pdmsNodes = new List<PdmsNode>();

            using (var reader = new StatefulReader(pdmsTxtFilePath))
            {
                var indentationStack = new Stack<int>();
                PdmsNode? currentPdmsNode = null;

                var headerInfo = ParseHeader(reader);
                string newItemSeparator = headerInfo.StartSeparator;
                int newItemSeparatorLength = newItemSeparator.Length;
                string endItemSeparator = headerInfo.EndSeparator;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                        throw new NullReferenceException( $"Unexpected null in {nameof(line)}");
                    
                    var trimmedLine = line.Trim();
                    if (trimmedLine.StartsWith(newItemSeparator))
                    {
                        var pdmsNode = new PdmsNode
                        (
                            Name: trimmedLine.Substring(newItemSeparatorLength).Trim(),
                            MetadataDict: new Dictionary<string, string>(),
                            Parent: currentPdmsNode,
                            Children: new List<PdmsNode>()
                        );

                        indentationStack.Push(line.Substring(0, line.IndexOf(newItemSeparator[0])).Length);

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
                            var indentation = line.Substring(0, line.IndexOf(endItemSeparator[0])).Length;
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
                            var (key, value) = SplitKeyValue(trimmedLine, headerInfo.NameEnd);
                            currentPdmsNode!.MetadataDict[key] = StripQuotes(value);
                        }
                    }
                }
            }

            return pdmsNodes;
        }

        private class PdmsFileHeader
        {
            public string FileFormat = "Unknown";
            public string StartSeparator = "NEW";
            public string EndSeparator = "END";
            public string NameEnd = ":=";
            public string Sep = "&end&";
            // ReSharper disable once CollectionNeverQueried.Local
            public readonly Dictionary<string, string> HeaderMetadata = new();
        }

        private static PdmsFileHeader ParseHeader(StreamReader reader)
        {
            PdmsFileHeader header = new PdmsFileHeader();
            
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
            if (!firstLineHeaderInformation.StartsWith($"{header.StartSeparator} Header Information"))
            {
                return header;
            }

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
                        var (key, value) = SplitKeyValue(keyValueSegment, header.NameEnd);
                        header.HeaderMetadata[key] = value;
                    }
                }
            }

            return header;
        }

        private static (string key, string value) SplitKeyValue(string keyValueSegment, string nameEnd)
        {
            var nameSeparatorIndex = keyValueSegment.IndexOf(nameEnd, StringComparison.InvariantCulture);
            var key = keyValueSegment.Substring(0, nameSeparatorIndex).Trim();
            var value = keyValueSegment.Substring(nameSeparatorIndex + nameEnd.Length).Trim();
            return (key, value);
        }
        

        private static string StripQuotes(string input)
        {
            if (string.IsNullOrEmpty(input) || input.Length < 2)
                return input;
            
            if (input.First() == input.Last() && Quotes.Any(q => q == input.First()))
                return input.Substring(1, input.Length - 2);
            
            return input;
        }
    }
}