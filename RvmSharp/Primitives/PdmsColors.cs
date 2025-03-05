namespace RvmSharp.Primitives;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

public static class PdmsColors
{
    private static readonly Dictionary<uint, string> PdmsIdToColorNameMap = new()
    {
        { 1, "Black" },
        { 2, "Red" },
        { 3, "Orange" },
        { 4, "Yellow" },
        { 5, "Green" },
        { 6, "Cyan" },
        { 7, "Blue" },
        { 8, "Magenta" },
        { 9, "Brown" },
        { 10, "White" },
        { 11, "Salmon" },
        { 12, "LightGrey" },
        { 13, "Grey" },
        { 14, "Plum" },
        { 15, "WhiteSmoke" },
        { 16, "Maroon" },
        { 17, "SpringGreen" },
        { 18, "Wheat" },
        { 19, "Gold" },
        { 20, "RoyalBlue" },
        { 21, "LightGold" },
        { 22, "DeepPink" },
        { 23, "ForestGreen" },
        { 24, "BrightOrange" },
        { 25, "Ivory" },
        { 26, "Chocolate" },
        { 27, "SteelBlue" },
        { 28, "White" },
        { 29, "Midnight" },
        { 30, "NavyBlue" },
        { 31, "Pink" },
        { 32, "CoralRed" },
        { 33, "Black" },
        { 34, "Red" },
        { 35, "Orange" },
        { 36, "Yellow" },
        { 37, "Green" },
        { 38, "Cyan" },
        { 39, "Blue" },
        { 40, "Magenta" },
        { 41, "Brown" },
        { 42, "White" },
        { 43, "Salmon" },
        { 44, "LightGrey" },
        { 45, "Grey" },
        { 46, "Plum" },
        { 47, "WhiteSmoke" },
        { 48, "Maroon" },
        { 49, "SpringGreen" },
        { 50, "Wheat" },
        { 51, "Gold" },
        { 52, "RoyalBlue" },
        { 53, "LightGold" },
        { 54, "DeepPink" },
        { 55, "ForestGreen" },
        { 56, "BrightOrange" },
        { 57, "Ivory" },
        { 58, "Chocolate" },
        { 59, "SteelBlue" },
        { 60, "White" },
        { 61, "Midnight" },
        { 62, "NavyBlue" },
        { 63, "Pink" },
        { 64, "CoralRed" },
        { 206, "Black" },
        { 207, "White" },
        { 208, "WhiteSmoke" },
        { 209, "Ivory" },
        { 210, "Grey" },
        { 211, "LightGrey" },
        { 212, "DarkGrey" },
        { 213, "DarkSlate" },
        { 214, "Red" },
        { 215, "BrightRed" },
        { 216, "CoralRed" },
        { 217, "Tomato" },
        { 218, "Plum" },
        { 219, "DeepPink" },
        { 220, "Pink" },
        { 221, "Salmon" },
        { 222, "Orange" },
        { 223, "BrightOrange" },
        { 224, "OrangeRed" },
        { 225, "Maroon" },
        { 226, "Yellow" },
        { 227, "Gold" },
        { 228, "LightYellow" },
        { 229, "LightGold" },
        { 230, "YellowGreen" },
        { 231, "SpringGreen" },
        { 232, "Green" },
        { 233, "ForestGreen" },
        { 234, "DarkGreen" },
        { 235, "Cyan" },
        { 236, "Turquoise" },
        { 237, "Aquamarine" },
        { 238, "Blue" },
        { 239, "RoyalBlue" },
        { 240, "NavyBlue" },
        { 241, "PowderBlue" },
        { 242, "Midnight" },
        { 243, "SteelBlue" },
        { 244, "Indigo" },
        { 245, "Mauve" },
        { 246, "Violet" },
        { 247, "Magenta" },
        { 248, "Beige" },
        { 249, "Wheat" },
        { 250, "Tan" },
        { 251, "SandyBrown" },
        { 252, "Brown" },
        { 253, "Khaki" },
        { 254, "Chocolate" },
        { 255, "DarkBrown" },
    };

    private static readonly Dictionary<string, Color> PdmsColorNameToColorMap = new List<(
        string Name,
        (float R, float G, float B) color
    )>
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
        ("DarkBrown", (55 / 100.0f, 27 / 100.0f, 8 / 100.0f)),
    }
        .Select(x =>
        {
            var (name, (r, g, b)) = x;
            return (
                name,
                Color: Color.FromArgb(
                    alpha: byte.MaxValue,
                    red: (byte)(r * 255f),
                    green: (byte)(g * 255f),
                    blue: (byte)(b * 255f)
                )
            );
        })
        .ToDictionary(x => x.name, x => x.Color);

    private static readonly Dictionary<uint, Color> PdmsIdToColorMap = PdmsIdToColorNameMap.ToDictionary(
        x => x.Key,
        x => PdmsColorNameToColorMap[x.Value]
    );

    /// <summary>
    /// Get a Color from the Color Table based on a "Code" from RVM data.
    /// The Color Table can be different for different sources of the RVM file, and should be investigated.
    /// Not all RVM colors are mapped, and this will throw if the color is not found.
    /// </summary>
    /// <param name="code">RVM Color value</param>
    /// <param name="color"></param>
    /// <returns>bool</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static bool TryGetColorByCode(uint code, out Color color)
    {
        if (!PdmsIdToColorMap.TryGetValue(code, out var c))
        {
            color = default;
            return false;
        }

        color = c;
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
        if (PdmsColorNameToColorMap.TryGetValue(name, out var color))
        {
            return color;
        }

        throw new KeyNotFoundException($"There is no color named {name}");
    }
}
