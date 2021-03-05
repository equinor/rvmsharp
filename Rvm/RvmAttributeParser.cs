using System.IO;

namespace rvmsharp.Rvm
{
    public class RvmAttributeParser
    {
        private static void ParseIndentation(string line, out int spaces, out int tabs)
        {
            spaces = 0;
            tabs = 0;
            for (var i = 0; i < line.Length; i++)
            {
                switch (line[i])
                {
                    case ' ':
                        spaces++;
                        break;
                    case '\t':
                        tabs++;
                        break;
                    default:
                        i = line.Length;
                        break;
                }
            }
        }

        public RvmAttributeParser(Stream stream)
        {
            var reader = new StreamReader(stream);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                ParseIndentation(line, out var spaces, out var tabs);
                if (line.StartsWith("NEW ") || line.StartsWith("NEW\t"))
                {

                }
            }
        }
    }
}
