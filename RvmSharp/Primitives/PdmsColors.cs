namespace RvmSharp.Primitives
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.Linq;

    public static class PdmsColors
    {
        private static readonly IReadOnlyList<(string Name, Color Color)> PdmsColorsList =
            new List<(string Name, (float R, float G, float B) color)>()
            {
                ("Black", (0 / 100.0f, 0 / 100.0f, 0 / 100.0f)),
                ("White", (100 / 100.0f, 100 / 100.0f, 100 / 100.0f)),
                ("WhiteSmoke", (96 / 100.0f, 96 / 100.0f, 96 / 100.0f)),
                ("Ivory", (93 / 100.0f, 93 / 100.0f, 88 / 100.0f)),
                ("Grey", (66 / 100.0f, 66 / 100.0f, 66 / 100.0f)),
                ("LightGrey", (75 / 100.0f, 75 / 100.0f, 75 / 100.0f)),
                ("DarkGrey", (32 / 100.0f, 55 / 100.0f, 55 / 100.0f)),
                ("DarkSlate", (18 / 100.0f, 31 / 100.0f, 31 / 100.0f)),
                ("Red", (80 / 100.0f, 0 / 100.0f, 0 / 100.0f)),
                ("BrightRed", (100 / 100.0f, 0 / 100.0f, 0 / 100.0f)),
                ("CoralRed", (80 / 100.0f, 36 / 100.0f, 27 / 100.0f)),
                ("Tomato", (100 / 100.0f, 39 / 100.0f, 28 / 100.0f)),
                ("Plum", (55 / 100.0f, 40 / 100.0f, 55 / 100.0f)),
                ("DeepPink", (93 / 100.0f, 7 / 100.0f, 54 / 100.0f)),
                ("Pink", (80 / 100.0f, 57 / 100.0f, 62 / 100.0f)),
                ("Salmon", (98 / 100.0f, 50 / 100.0f, 44 / 100.0f)),
                ("Orange", (93 / 100.0f, 60 / 100.0f, 0 / 100.0f)),
                ("BrightOrange", (100 / 100.0f, 65 / 100.0f, 0 / 100.0f)),
                ("OrangeRed", (100 / 100.0f, 50 / 100.0f, 0 / 100.0f)),
                ("Maroon", (56 / 100.0f, 14 / 100.0f, 42 / 100.0f)),
                ("Yellow", (80 / 100.0f, 80 / 100.0f, 0 / 100.0f)),
                ("Gold", (93 / 100.0f, 79 / 100.0f, 20 / 100.0f)),
                ("LightYellow", (93 / 100.0f, 93 / 100.0f, 82 / 100.0f)),
                ("LightGold", (93 / 100.0f, 91 / 100.0f, 67 / 100.0f)),
                ("YellowGreen", (60 / 100.0f, 80 / 100.0f, 20 / 100.0f)),
                ("SpringGreen", (0 / 100.0f, 100 / 100.0f, 50 / 100.0f)),
                ("Green", (0 / 100.0f, 80 / 100.0f, 0 / 100.0f)),
                ("ForestGreen", (14 / 100.0f, 56 / 100.0f, 14 / 100.0f)),
                ("DarkGreen", (18 / 100.0f, 31 / 100.0f, 18 / 100.0f)),
                ("Cyan", (0 / 100.0f, 93 / 100.0f, 93 / 100.0f)),
                ("Turquoise", (0 / 100.0f, 75 / 100.0f, 80 / 100.0f)),
                ("Aquamarine", (46 / 100.0f, 93 / 100.0f, 78 / 100.0f)),
                ("Blue", (0 / 100.0f, 0 / 100.0f, 80 / 100.0f)),
                ("RoyalBlue", (28 / 100.0f, 46 / 100.0f, 100 / 100.0f)),
                ("NavyBlue", (0 / 100.0f, 0 / 100.0f, 50 / 100.0f)),
                ("PowderBlue", (69 / 100.0f, 88 / 100.0f, 90 / 100.0f)),
                ("Midnight", (18 / 100.0f, 18 / 100.0f, 31 / 100.0f)),
                ("SteelBlue", (28 / 100.0f, 51 / 100.0f, 71 / 100.0f)),
                ("Indigo", (20 / 100.0f, 0 / 100.0f, 40 / 100.0f)),
                ("Mauve", (40 / 100.0f, 0 / 100.0f, 60 / 100.0f)),
                ("Violet", (93 / 100.0f, 51 / 100.0f, 93 / 100.0f)),
                ("Magenta", (87 / 100.0f, 0 / 100.0f, 87 / 100.0f)),
                ("Beige", (96 / 100.0f, 96 / 100.0f, 86 / 100.0f)),
                ("Wheat", (96 / 100.0f, 87 / 100.0f, 70 / 100.0f)),
                ("Tan", (86 / 100.0f, 58 / 100.0f, 44 / 100.0f)),
                ("SandyBrown", (96 / 100.0f, 65 / 100.0f, 37 / 100.0f)),
                ("Brown", (80 / 100.0f, 17 / 100.0f, 17 / 100.0f)),
                ("Khaki", (62 / 100.0f, 62 / 100.0f, 37 / 100.0f)),
                ("Chocolate", (93 / 100.0f, 46 / 100.0f, 13 / 100.0f)),
                ("DarkBrown", (55 / 100.0f, 27 / 100.0f, 8 / 100.0f))
            }.Select(x =>
            {
                const byte alpha = byte.MaxValue;
                (var name, (float r, float g, float b)) = x;
                return (name,
                    Color: Color.FromArgb(
                        alpha: alpha,
                        red: (byte)r * 255,
                        green: (byte)g * 255,
                        blue: (byte)b * 255));
            }).ToList();

        /// <summary>
        /// Get a Color from the Color Table based on a "Code" from RVM data.
        /// The Color Table can be different for different sources of the RVM file, and should be investigated.
        /// Not all RVM colors are mapped, and this will throw if the color is not found.
        /// </summary>
        /// <param name="code">RVM Color value</param>
        /// <returns>Color</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static bool TryGetColorByCode(uint code, out Color color)
        {
            if (code < 1 || code > PdmsColorsList.Count)
            {
                color = default;
                return false;
            }

            var index = (int)code - 1;
            color = PdmsColorsList[index].Color;
            return true;
        }

        /// <summary>
        /// Find a color by its name. Will throw key not found exception for non-existing colors.
        /// </summary>
        /// <param name="name">The name. Ignores case.</param>
        /// <returns>Color</returns>
        /// <exception cref="KeyNotFoundException"></exception>
        // ReSharper disable once UnusedMember.Global
        public static Color GetColorByName(string name)
        {
            var match = PdmsColorsList.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (match == default)
                throw new KeyNotFoundException($"There is no color named {name}");

            return match.Color;
        }
    }
}