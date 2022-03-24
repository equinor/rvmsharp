namespace CadRevealComposer.Debug
{
    using CadRevealComposer.Operations;
    using CadRevealComposer.Primitives;
    using CadRevealComposer.Utils;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Reflection;
    using static CadRevealComposer.Operations.SectorSplitter;

    internal static class Coloring
    {
        internal static ProtoSector[] ColorSectors(ProtoSector[] sectors)
        {
            var colorList = GetListOfColors();

            for (int i = 0; i < sectors.Length; i++)
            {
                sectors[i] = ColorSector(sectors[i], colorList[i * 7 % colorList.Count]);
            }

            return sectors;
        }

        internal static ProtoSector[] ColorSectorsAtDepth(ProtoSector[] sectors, int depth)
        {
            var colorList = GetListOfColors();

            for (int i = 0; i < sectors.Length; i++)
            {
                if (sectors[i].Depth == depth)
                    sectors[i] = ColorSector(sectors[i], colorList[i * 7 % colorList.Count]);
                else
                    sectors[i] = ColorSector(sectors[i], Color.White);
            }

            return sectors;
        }

        private static ProtoSector[] ColorDepths(ProtoSector[] sectors)
        {
            var colorList = GetListOfColors();
            int count = 0;

            for (int i = 1; i < sectors.Length; i++)
            {
                (sectors, count) = ColorDepth(sectors, i, colorList[i * 7 % colorList.Count]);
                if (count == 0)
                    break;
            }

            return sectors;
        }

        private static (ProtoSector[], int) ColorDepth(ProtoSector[] sectors, int depth, Color color)
        {
            int count = 0;
            for (int i = 0; i < sectors.Length; i++)
            {
                if (sectors[i].Depth == depth)
                {
                    sectors[i] = ColorSector(sectors[i], color);
                    count++;
                }
            }

            return (sectors, count);
        }

        private static ProtoSector ColorSector(ProtoSector sector, Color color)
        {
            var newGeometries = sector.Geometries.Select(prop => prop with { Color = color }).ToArray();
            return sector with { Geometries = newGeometries };
        }

        internal static ProtoSector[] MergeToOneSector(ProtoSector[] sectors)
        {
            var geometriesList = new List<APrimitive>();

            foreach (var sector in sectors)
            {
                geometriesList.AddRange(sector.Geometries);
            }

            var geometries = geometriesList.ToArray();
            uint sectorId = sectors[0].SectorId;
            int depth = 1;
            string path = sectors[0].Path;
            var bbMin = geometries.GetBoundingBoxMin();
            var bbMax = geometries.GetBoundingBoxMax();

            var mergedSector = new ProtoSector(sectorId, null, depth, path, geometries, bbMin, bbMax);

            return new ProtoSector[1] { mergedSector };
        }
        private static List<Color> GetListOfColors()
        {
            var colorList = new List<Color>();

            foreach (var prop in typeof(Color).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                if (prop.PropertyType == typeof(Color))
                {
                    var value = prop.GetValue(null);
                    if (value != null)
                        colorList.Add((Color)value);
                }
            }

            return colorList;
        }
    }
}
