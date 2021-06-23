namespace RvmSharp.Primitives
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Globalization;
    using System.Linq;

    public static class PdmsColors
    {
        private static readonly List<(string name, float[] color)> PdmsColorsList = new()
        {
            ("Black", new[] { 0 / 100.0f, 0 / 100.0f, 0 / 100.0f }),
            ("White", new[] { 100 / 100.0f, 100 / 100.0f, 100 / 100.0f }),
            ("WhiteSmoke", new[] { 96 / 100.0f, 96 / 100.0f, 96 / 100.0f }),
            ("Ivory", new[] { 93 / 100.0f, 93 / 100.0f, 88 / 100.0f }),
            ("Grey", new[] { 66 / 100.0f, 66 / 100.0f, 66 / 100.0f }),
            ("LightGrey", new[] { 75 / 100.0f, 75 / 100.0f, 75 / 100.0f }),
            ("DarkGrey", new[] { 32 / 100.0f, 55 / 100.0f, 55 / 100.0f }),
            ("DarkSlate", new[] { 18 / 100.0f, 31 / 100.0f, 31 / 100.0f }),
            ("Red", new[] { 80 / 100.0f, 0 / 100.0f, 0 / 100.0f }),
            ("BrightRed", new[] { 100 / 100.0f, 0 / 100.0f, 0 / 100.0f }),
            ("CoralRed", new[] { 80 / 100.0f, 36 / 100.0f, 27 / 100.0f }),
            ("Tomato", new[] { 100 / 100.0f, 39 / 100.0f, 28 / 100.0f }),
            ("Plum", new[] { 55 / 100.0f, 40 / 100.0f, 55 / 100.0f }),
            ("DeepPink", new[] { 93 / 100.0f, 7 / 100.0f, 54 / 100.0f }),
            ("Pink", new[] { 80 / 100.0f, 57 / 100.0f, 62 / 100.0f }),
            ("Salmon", new[] { 98 / 100.0f, 50 / 100.0f, 44 / 100.0f }),
            ("Orange", new[] { 93 / 100.0f, 60 / 100.0f, 0 / 100.0f }),
            ("BrightOrange", new[] { 100 / 100.0f, 65 / 100.0f, 0 / 100.0f }),
            ("OrangeRed", new[] { 100 / 100.0f, 50 / 100.0f, 0 / 100.0f }),
            ("Maroon", new[] { 56 / 100.0f, 14 / 100.0f, 42 / 100.0f }),
            ("Yellow", new[] { 80 / 100.0f, 80 / 100.0f, 0 / 100.0f }),
            ("Gold", new[] { 93 / 100.0f, 79 / 100.0f, 20 / 100.0f }),
            ("LightYellow", new[] { 93 / 100.0f, 93 / 100.0f, 82 / 100.0f }),
            ("LightGold", new[] { 93 / 100.0f, 91 / 100.0f, 67 / 100.0f }),
            ("YellowGreen", new[] { 60 / 100.0f, 80 / 100.0f, 20 / 100.0f }),
            ("SpringGreen", new[] { 0 / 100.0f, 100 / 100.0f, 50 / 100.0f }),
            ("Green", new[] { 0 / 100.0f, 80 / 100.0f, 0 / 100.0f }),
            ("ForestGreen", new[] { 14 / 100.0f, 56 / 100.0f, 14 / 100.0f }),
            ("DarkGreen", new[] { 18 / 100.0f, 31 / 100.0f, 18 / 100.0f }),
            ("Cyan", new[] { 0 / 100.0f, 93 / 100.0f, 93 / 100.0f }),
            ("Turquoise", new[] { 0 / 100.0f, 75 / 100.0f, 80 / 100.0f }),
            ("Aquamarine", new[] { 46 / 100.0f, 93 / 100.0f, 78 / 100.0f }),
            ("Blue", new[] { 0 / 100.0f, 0 / 100.0f, 80 / 100.0f }),
            ("RoyalBlue", new[] { 28 / 100.0f, 46 / 100.0f, 100 / 100.0f }),
            ("NavyBlue", new[] { 0 / 100.0f, 0 / 100.0f, 50 / 100.0f }),
            ("PowderBlue", new[] { 69 / 100.0f, 88 / 100.0f, 90 / 100.0f }),
            ("Midnight", new[] { 18 / 100.0f, 18 / 100.0f, 31 / 100.0f }),
            ("SteelBlue", new[] { 28 / 100.0f, 51 / 100.0f, 71 / 100.0f }),
            ("Indigo", new[] { 20 / 100.0f, 0 / 100.0f, 40 / 100.0f }),
            ("Mauve", new[] { 40 / 100.0f, 0 / 100.0f, 60 / 100.0f }),
            ("Violet", new[] { 93 / 100.0f, 51 / 100.0f, 93 / 100.0f }),
            ("Magenta", new[] { 87 / 100.0f, 0 / 100.0f, 87 / 100.0f }),
            ("Beige", new[] { 96 / 100.0f, 96 / 100.0f, 86 / 100.0f }),
            ("Wheat", new[] { 96 / 100.0f, 87 / 100.0f, 70 / 100.0f }),
            ("Tan", new[] { 86 / 100.0f, 58 / 100.0f, 44 / 100.0f }),
            ("SandyBrown", new[] { 96 / 100.0f, 65 / 100.0f, 37 / 100.0f }),
            ("Brown", new[] { 80 / 100.0f, 17 / 100.0f, 17 / 100.0f }),
            ("Khaki", new[] { 62 / 100.0f, 62 / 100.0f, 37 / 100.0f }),
            ("Chocolate", new[] { 93 / 100.0f, 46 / 100.0f, 13 / 100.0f }),
            ("DarkBrown", new[] { 55 / 100.0f, 27 / 100.0f, 8 / 100.0f })
        };

        public static float[] GetColorByCode(uint code)
        {
            if (code < 1 || code > PdmsColorsList.Count)
                throw new ArgumentOutOfRangeException(
                    $"Color code must be between 1 and {PdmsColorsList.Count} inclusive, got: {code}");

            var index = (int)code - 1;

            return PdmsColorsList[index].color;
        }

        public static byte[] GetColorAsBytesByCode(uint code)
        {
            var color = GetColorByCode(code);

            var colorAsByte = color.Select(x => (byte)Math.Round((x * 255)));
            var colorAsByteWithAlpha = colorAsByte.ToList();
            colorAsByteWithAlpha.Add(255);

            if (colorAsByteWithAlpha.Count != 4)
                throw new Exception("Expected 4 element of color, got " + colorAsByteWithAlpha.Count);

            return colorAsByteWithAlpha.ToArray();
        }

        public static Color GetColorAsColorByCode(uint code)
        {
            var colorFloat = GetColorByCode(code);
            var colorAsByte = colorFloat.Select(x => (byte)Math.Round((x * 255))).ToArray();

            if (colorAsByte.Length != 3)
                throw new Exception("Expected 3 element of color.");

            var color = Color.FromArgb(alpha: 255, red: colorAsByte[0], green: colorAsByte[1], blue: colorAsByte[2]);
            return color;
        }


        public static float[] GetColorByName(string name)
        {
            var match = PdmsColorsList.FirstOrDefault(p =>
                p.name.ToLower(CultureInfo.InvariantCulture) == name.ToLower(CultureInfo.InvariantCulture));
            if (match.color == null)
                throw new KeyNotFoundException($"There is no color named {name.ToLower()}");
            return match.color;
        }
    }
}