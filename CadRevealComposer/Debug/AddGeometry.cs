namespace CadRevealComposer.Debug
{
    using CadRevealComposer.Primitives;
    using RvmSharp.Primitives;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Numerics;
    using static CadRevealComposer.Operations.SectorSplitter;

    internal static class AddGeometry
    {
        internal static ProtoSector[] AddBoxesForSectors(ProtoSector[] sectors)
        {
            for (int i = 1; i < sectors.Length; i++)
            {
                sectors[i] = AddBoxForSector(sectors[i], Color.Red);
            }

            return sectors;
        }

        internal static ProtoSector AddBoxForSector(ProtoSector sector, Color color)
        {
            var bbmin = sector.BoundingBoxMin;
            var bbmax = sector.BoundingBoxMax;

            var center = (bbmin + bbmax) / 2;
            float sizeX = bbmax.X - bbmin.X;
            float sizeY = bbmax.Y - bbmin.Y;
            float sizeZ = bbmax.Z - bbmin.Z;

            var newGeometries = new List<APrimitive>(sector.Geometries);
            newGeometries.Add(CreateBox(center, new Vector3(sizeX, sizeY, sizeZ), color));

            return sector with { Geometries = newGeometries.ToArray() };
        }

        internal static ProtoSector[] AddWireframeBoxForSectors(ProtoSector[] sectors)
        {
            for (int i = 1; i < sectors.Length; i++)
            {
                sectors[i] = AddWireframeBoxForSector(sectors[i], Color.Red);
            }

            return sectors;
        }

        internal static ProtoSector AddWireframeBoxForSector(ProtoSector sector, Color color)
        {
            float xMin = sector.BoundingBoxMin.X;
            float yMin = sector.BoundingBoxMin.Y;
            float zMin = sector.BoundingBoxMin.Z;
            float xMax = sector.BoundingBoxMax.X;
            float yMax = sector.BoundingBoxMax.Y;
            float zMax = sector.BoundingBoxMax.Z;

            var box1 = CreateBox(new Vector3((xMax + xMin) / 2, yMin, zMin), new Vector3(xMax - xMin, 0.1f, 0.1f), color);
            var box2 = CreateBox(new Vector3((xMax + xMin) / 2, yMax, zMin), new Vector3(xMax - xMin, 0.1f, 0.1f), color);
            var box3 = CreateBox(new Vector3(xMin, (yMin + yMax) / 2, zMin), new Vector3(0.1f, yMax - yMin, 0.1f), color);
            var box4 = CreateBox(new Vector3(xMax, (yMin + yMax) / 2, zMin), new Vector3(0.1f, yMax - yMin, 0.1f), color);

            var newGeometries = new List<APrimitive>(sector.Geometries);
            newGeometries.Add(box1);
            newGeometries.Add(box2);
            newGeometries.Add(box3);
            newGeometries.Add(box4);

            return sector with { Geometries = newGeometries.ToArray() };
        }

        private static APrimitive CreateBox(Vector3 position, Vector3 size, Color color)
        {
            var commonProperties = new CommonPrimitiveProperties(
                0,
                0,
                position,
                Quaternion.Identity,
                new Vector3(1),
                0,
                new RvmBoundingBox(position - size / 2, position + size / 2),
                color,
                (new Vector3(0, 0, 1), 0),
                null);

            return new Box(commonProperties, new Vector3(0, 0, 1), size.X, size.Y, size.Z, 0f);
        }
    }
}
