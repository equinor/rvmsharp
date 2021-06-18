namespace CadRevealComposer.Writers
{
    using Primitives;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public static class I3dWriter
    {
        private const uint AttributeCount = 18;

        public static void WriteSector(FileSector sector, Stream stream)
        {
            if (!stream.CanSeek)
                throw new Exception("Cannot seek");
            stream.WriteUint32(0); // size will be set at the end
            WriteHeader(sector.Header, stream);

            if (sector.Header.Attributes != null)
                WritePrimitives(sector.PrimitiveCollections, sector.Header.Attributes, stream);

            var position = stream.Position;
            stream.Seek(0, SeekOrigin.Begin);
            stream.WriteUint32((uint)(position - 4));
            stream.Flush();
        }

        private static void WritePrimitives(PrimitiveCollections sectorPrimitiveCollections,
            Attributes headerAttributes, Stream stream)
        {
            {
                var boxCollection = sectorPrimitiveCollections.BoxCollection;
                if (boxCollection.Length > 0)
                {
                    var indices = new List<ulong>();
                    foreach (var box in boxCollection)
                    {
                        indices.Add(box.TreeIndex);
                        indices.Add(GetColorIndex(box.Color, headerAttributes.Color));
                        indices.Add(GetFloatIndex(box.Diagonal, headerAttributes.Diagonal));
                        indices.Add(GetFloatIndex(box.CenterX, headerAttributes.CenterX));
                        indices.Add(GetFloatIndex(box.CenterY, headerAttributes.CenterY));
                        indices.Add(GetFloatIndex(box.CenterZ, headerAttributes.CenterZ));
                        indices.Add(GetNormalIndex(box.Normal, headerAttributes.Normal));
                        indices.Add(GetFloatIndex(box.DeltaX, headerAttributes.Delta));
                        indices.Add(GetFloatIndex(box.DeltaY, headerAttributes.Delta));
                        indices.Add(GetFloatIndex(box.DeltaZ, headerAttributes.Delta));
                        indices.Add(GetFloatIndex(box.RotationAngle, headerAttributes.Angle));
                    }

                    var nodeIds = boxCollection.Select(b => b.NodeId).ToArray();
                    WritePrimitiveCollection(stream, 1, nodeIds, indices.ToArray());
                }
            }

            {
                var circleCollection = sectorPrimitiveCollections.CircleCollection;
                if (circleCollection.Length > 0)
                {
                    var indices = new List<ulong>();
                    foreach (var circle in circleCollection)
                    {
                        indices.Add(circle.TreeIndex);
                        indices.Add(GetColorIndex(circle.Color, headerAttributes.Color));
                        indices.Add(GetFloatIndex(circle.Diagonal, headerAttributes.Diagonal));
                        indices.Add(GetFloatIndex(circle.CenterX, headerAttributes.CenterX));
                        indices.Add(GetFloatIndex(circle.CenterY, headerAttributes.CenterY));
                        indices.Add(GetFloatIndex(circle.CenterZ, headerAttributes.CenterZ));
                        indices.Add(GetNormalIndex(circle.Normal, headerAttributes.Normal));
                        indices.Add(GetFloatIndex(circle.Radius, headerAttributes.Radius));
                    }

                    var nodeIds = circleCollection.Select(b => b.NodeId).ToArray();
                    WritePrimitiveCollection(stream, 2, nodeIds, indices.ToArray());
                }
            }

            {
                var closedConeCollection = sectorPrimitiveCollections.ClosedConeCollection;
                if (closedConeCollection.Length > 0)
                {
                    var indices = new List<ulong>();
                    foreach (var geometry in closedConeCollection)
                    {
                        indices.Add(geometry.TreeIndex);
                        indices.Add(GetColorIndex(geometry.Color, headerAttributes.Color));
                        indices.Add(GetFloatIndex(geometry.Diagonal, headerAttributes.Diagonal));
                        indices.Add(GetFloatIndex(geometry.CenterX, headerAttributes.CenterX));
                        indices.Add(GetFloatIndex(geometry.CenterY, headerAttributes.CenterY));
                        indices.Add(GetFloatIndex(geometry.CenterZ, headerAttributes.CenterZ));
                        indices.Add(GetNormalIndex(geometry.CenterAxis, headerAttributes.Normal));
                        indices.Add(GetFloatIndex(geometry.Height, headerAttributes.Height));
                        indices.Add(GetFloatIndex(geometry.RadiusA, headerAttributes.Radius));
                        indices.Add(GetFloatIndex(geometry.RadiusB, headerAttributes.Radius));
                    }

                    var nodeIds = closedConeCollection.Select(b => b.NodeId).ToArray();
                    WritePrimitiveCollection(stream, 3, nodeIds, indices.ToArray());
                }
            }

            var closedCylinderCollection = sectorPrimitiveCollections.ClosedCylinderCollection;
            if (closedCylinderCollection.Length > 0)
            {
                var indices = new List<ulong>();
                foreach (var closedCylinder in closedCylinderCollection)
                {
                    indices.Add(closedCylinder.TreeIndex);
                    indices.Add(GetColorIndex(closedCylinder.Color, headerAttributes.Color));
                    indices.Add(GetFloatIndex(closedCylinder.Diagonal, headerAttributes.Diagonal));
                    indices.Add(GetFloatIndex(closedCylinder.CenterX, headerAttributes.CenterX));
                    indices.Add(GetFloatIndex(closedCylinder.CenterY, headerAttributes.CenterY));
                    indices.Add(GetFloatIndex(closedCylinder.CenterZ, headerAttributes.CenterZ));
                    indices.Add(GetNormalIndex(closedCylinder.CenterAxis, headerAttributes.Normal));
                    indices.Add(GetFloatIndex(closedCylinder.Height, headerAttributes.Height));
                    indices.Add(GetFloatIndex(closedCylinder.Radius, headerAttributes.Radius));
                }

                var nodeIds = closedCylinderCollection.Select(b => b.NodeId).ToArray();
                WritePrimitiveCollection(stream, 4, nodeIds, indices.ToArray());
            }

            var closedEccentricConeCollection =
                sectorPrimitiveCollections.ClosedEccentricConeCollection;
            if (closedEccentricConeCollection.Length > 0)
            {
                var indices = new List<ulong>();
                foreach (var geometry in closedEccentricConeCollection)
                {
                    indices.Add(geometry.TreeIndex);
                    indices.Add(GetColorIndex(geometry.Color, headerAttributes.Color));
                    indices.Add(GetFloatIndex(geometry.Diagonal, headerAttributes.Diagonal));
                    indices.Add(GetFloatIndex(geometry.CenterX, headerAttributes.CenterX));
                    indices.Add(GetFloatIndex(geometry.CenterY, headerAttributes.CenterY));
                    indices.Add(GetFloatIndex(geometry.CenterZ, headerAttributes.CenterZ));
                    indices.Add(GetNormalIndex(geometry.CenterAxis, headerAttributes.Normal));
                    indices.Add(GetFloatIndex(geometry.Height, headerAttributes.Height));
                    indices.Add(GetFloatIndex(geometry.RadiusA, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.RadiusB, headerAttributes.Radius));
                    indices.Add(GetNormalIndex(geometry.CapNormal, headerAttributes.Normal));
                }

                var nodeIds = closedEccentricConeCollection.Select(b => b.NodeId).ToArray();
                WritePrimitiveCollection(stream, 5, nodeIds, indices.ToArray());
            }

            var closedEllipsoidSegmentCollection =
                sectorPrimitiveCollections.ClosedEllipsoidSegmentCollection;
            if (closedEllipsoidSegmentCollection.Length > 0)
            {
                var indices = new List<ulong>();
                foreach (var geometry in closedEllipsoidSegmentCollection)
                {
                    indices.Add(geometry.TreeIndex);
                    indices.Add(GetColorIndex(geometry.Color, headerAttributes.Color));
                    indices.Add(GetFloatIndex(geometry.Diagonal, headerAttributes.Diagonal));
                    indices.Add(GetFloatIndex(geometry.CenterX, headerAttributes.CenterX));
                    indices.Add(GetFloatIndex(geometry.CenterY, headerAttributes.CenterY));
                    indices.Add(GetFloatIndex(geometry.CenterZ, headerAttributes.CenterZ));
                    indices.Add(GetNormalIndex(geometry.Normal, headerAttributes.Normal));
                    indices.Add(GetFloatIndex(geometry.Height, headerAttributes.Height));
                    indices.Add(GetFloatIndex(geometry.HorizontalRadius, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.VerticalRadius, headerAttributes.Radius));
                }

                var nodeIds = closedEllipsoidSegmentCollection.Select(b => b.NodeId).ToArray();
                WritePrimitiveCollection(stream, 6, nodeIds, indices.ToArray());
            }

            var closedExtrudedRingSegmentCollection =
                sectorPrimitiveCollections.ClosedExtrudedRingSegmentCollection;
            if (closedExtrudedRingSegmentCollection.Length > 0)
            {
                var indices = new List<ulong>();
                foreach (var geometry in closedExtrudedRingSegmentCollection)
                {
                    indices.Add(geometry.TreeIndex);
                    indices.Add(GetColorIndex(geometry.Color, headerAttributes.Color));
                    indices.Add(GetFloatIndex(geometry.Diagonal, headerAttributes.Diagonal));
                    indices.Add(GetFloatIndex(geometry.CenterX, headerAttributes.CenterX));
                    indices.Add(GetFloatIndex(geometry.CenterY, headerAttributes.CenterY));
                    indices.Add(GetFloatIndex(geometry.CenterZ, headerAttributes.CenterZ));
                    indices.Add(GetNormalIndex(geometry.CenterAxis, headerAttributes.Normal));
                    indices.Add(GetFloatIndex(geometry.Height, headerAttributes.Height));
                    indices.Add(GetFloatIndex(geometry.InnerRadius, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.OuterRadius, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.RotationAngle, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.ArcAngle, headerAttributes.Angle));
                }

                var nodeIds = closedExtrudedRingSegmentCollection.Select(b => b.NodeId).ToArray();
                WritePrimitiveCollection(stream, 7, nodeIds, indices.ToArray());
            }

            var closedSphericalSegmentCollection =
                sectorPrimitiveCollections.ClosedSphericalSegmentCollection;
            if (closedSphericalSegmentCollection.Length > 0)
            {
                var indices = new List<ulong>();
                foreach (var geometry in closedSphericalSegmentCollection)
                {
                    indices.Add(geometry.TreeIndex);
                    indices.Add(GetColorIndex(geometry.Color, headerAttributes.Color));
                    indices.Add(GetFloatIndex(geometry.Diagonal, headerAttributes.Diagonal));
                    indices.Add(GetFloatIndex(geometry.CenterX, headerAttributes.CenterX));
                    indices.Add(GetFloatIndex(geometry.CenterY, headerAttributes.CenterY));
                    indices.Add(GetFloatIndex(geometry.CenterZ, headerAttributes.CenterZ));
                    indices.Add(GetNormalIndex(geometry.Normal, headerAttributes.Normal));
                    indices.Add(GetFloatIndex(geometry.Height, headerAttributes.Height));
                    indices.Add(GetFloatIndex(geometry.Radius, headerAttributes.Radius));
                }

                var nodeIds = closedSphericalSegmentCollection.Select(b => b.NodeId).ToArray();
                WritePrimitiveCollection(stream, 9, nodeIds, indices.ToArray());
            }

            var closedTorusSegmentCollection =
                sectorPrimitiveCollections.ClosedTorusSegmentCollection;
            if (closedTorusSegmentCollection.Length > 0)
            {
                var indices = new List<ulong>();
                foreach (var geometry in closedTorusSegmentCollection)
                {
                    indices.Add(geometry.TreeIndex);
                    indices.Add(GetColorIndex(geometry.Color, headerAttributes.Color));
                    indices.Add(GetFloatIndex(geometry.Diagonal, headerAttributes.Diagonal));
                    indices.Add(GetFloatIndex(geometry.CenterX, headerAttributes.CenterX));
                    indices.Add(GetFloatIndex(geometry.CenterY, headerAttributes.CenterY));
                    indices.Add(GetFloatIndex(geometry.CenterZ, headerAttributes.CenterZ));
                    indices.Add(GetNormalIndex(geometry.Normal, headerAttributes.Normal));
                    indices.Add(GetFloatIndex(geometry.Radius, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.TubeRadius, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.RotationAngle, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.ArcAngle, headerAttributes.Angle));
                }

                var nodeIds = closedTorusSegmentCollection.Select(b => b.NodeId).ToArray();
                WritePrimitiveCollection(stream, 10, nodeIds, indices.ToArray());
            }

            var ellipsoidCollection = sectorPrimitiveCollections.EllipsoidCollection;
            if (ellipsoidCollection.Length > 0)
            {
                var indices = new List<ulong>();
                foreach (var geometry in ellipsoidCollection)
                {
                    indices.Add(geometry.TreeIndex);
                    indices.Add(GetColorIndex(geometry.Color, headerAttributes.Color));
                    indices.Add(GetFloatIndex(geometry.Diagonal, headerAttributes.Diagonal));
                    indices.Add(GetFloatIndex(geometry.CenterX, headerAttributes.CenterX));
                    indices.Add(GetFloatIndex(geometry.CenterY, headerAttributes.CenterY));
                    indices.Add(GetFloatIndex(geometry.CenterZ, headerAttributes.CenterZ));
                    indices.Add(GetNormalIndex(geometry.Normal, headerAttributes.Normal));
                    indices.Add(GetFloatIndex(geometry.HorizontalRadius, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.VerticalRadius, headerAttributes.Radius));
                }

                var nodeIds = ellipsoidCollection.Select(b => b.NodeId).ToArray();
                WritePrimitiveCollection(stream, 11, nodeIds, indices.ToArray());
            }

            var extrudedRingCollection = sectorPrimitiveCollections.ExtrudedRingCollection;
            if (extrudedRingCollection.Length > 0)
            {
                var indices = new List<ulong>();
                foreach (var geometry in extrudedRingCollection)
                {
                    indices.Add(geometry.TreeIndex);
                    indices.Add(GetColorIndex(geometry.Color, headerAttributes.Color));
                    indices.Add(GetFloatIndex(geometry.Diagonal, headerAttributes.Diagonal));
                    indices.Add(GetFloatIndex(geometry.CenterX, headerAttributes.CenterX));
                    indices.Add(GetFloatIndex(geometry.CenterY, headerAttributes.CenterY));
                    indices.Add(GetFloatIndex(geometry.CenterZ, headerAttributes.CenterZ));
                    indices.Add(GetNormalIndex(geometry.CenterAxis, headerAttributes.Normal));
                    indices.Add(GetFloatIndex(geometry.Height, headerAttributes.Height));
                    indices.Add(GetFloatIndex(geometry.InnerRadius, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.OuterRadius, headerAttributes.Radius));
                }

                var nodeIds = extrudedRingCollection.Select(b => b.NodeId).ToArray();
                WritePrimitiveCollection(stream, 12, nodeIds, indices.ToArray());
            }

            var nutCollection = sectorPrimitiveCollections.NutCollection;
            if (nutCollection.Length > 0)
            {
                var indices = new List<ulong>();
                foreach (var geometry in nutCollection)
                {
                    indices.Add(geometry.TreeIndex);
                    indices.Add(GetColorIndex(geometry.Color, headerAttributes.Color));
                    indices.Add(GetFloatIndex(geometry.Diagonal, headerAttributes.Diagonal));
                    indices.Add(GetFloatIndex(geometry.CenterX, headerAttributes.CenterX));
                    indices.Add(GetFloatIndex(geometry.CenterY, headerAttributes.CenterY));
                    indices.Add(GetFloatIndex(geometry.CenterZ, headerAttributes.CenterZ));
                    indices.Add(GetNormalIndex(geometry.CenterAxis, headerAttributes.Normal));
                    indices.Add(GetFloatIndex(geometry.Height, headerAttributes.Height));
                    indices.Add(GetFloatIndex(geometry.Radius, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.RotationAngle, headerAttributes.Angle));
                }

                var nodeIds = nutCollection.Select(b => b.NodeId).ToArray();
                WritePrimitiveCollection(stream, 13, nodeIds, indices.ToArray());
            }

            var openConeCollection = sectorPrimitiveCollections.OpenConeCollection;
            if (openConeCollection.Length > 0)
            {
                var indices = new List<ulong>();
                foreach (var geometry in openConeCollection)
                {
                    indices.Add(geometry.TreeIndex);
                    indices.Add(GetColorIndex(geometry.Color, headerAttributes.Color));
                    indices.Add(GetFloatIndex(geometry.Diagonal, headerAttributes.Diagonal));
                    indices.Add(GetFloatIndex(geometry.CenterX, headerAttributes.CenterX));
                    indices.Add(GetFloatIndex(geometry.CenterY, headerAttributes.CenterY));
                    indices.Add(GetFloatIndex(geometry.CenterZ, headerAttributes.CenterZ));
                    indices.Add(GetNormalIndex(geometry.CenterAxis, headerAttributes.Normal));
                    indices.Add(GetFloatIndex(geometry.Height, headerAttributes.Height));
                    indices.Add(GetFloatIndex(geometry.RadiusA, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.RadiusB, headerAttributes.Radius));
                }

                var nodeIds = openConeCollection.Select(b => b.NodeId).ToArray();
                WritePrimitiveCollection(stream, 14, nodeIds, indices.ToArray());
            }

            var openCylinderCollection = sectorPrimitiveCollections.OpenCylinderCollection;
            if (openCylinderCollection.Length > 0)
            {
                var indices = new List<ulong>();
                foreach (var geometry in openCylinderCollection)
                {
                    indices.Add(geometry.TreeIndex);
                    indices.Add(GetColorIndex(geometry.Color, headerAttributes.Color));
                    indices.Add(GetFloatIndex(geometry.Diagonal, headerAttributes.Diagonal));
                    indices.Add(GetFloatIndex(geometry.CenterX, headerAttributes.CenterX));
                    indices.Add(GetFloatIndex(geometry.CenterY, headerAttributes.CenterY));
                    indices.Add(GetFloatIndex(geometry.CenterZ, headerAttributes.CenterZ));
                    indices.Add(GetNormalIndex(geometry.CenterAxis, headerAttributes.Normal));
                    indices.Add(GetFloatIndex(geometry.Height, headerAttributes.Height));
                    indices.Add(GetFloatIndex(geometry.Radius, headerAttributes.Radius));
                }

                var nodeIds = openCylinderCollection.Select(b => b.NodeId).ToArray();
                WritePrimitiveCollection(stream, 15, nodeIds, indices.ToArray());
            }

            var openEccentricConeCollection = sectorPrimitiveCollections.OpenEccentricConeCollection;
            if (openEccentricConeCollection.Length > 0)
            {
                var indices = new List<ulong>();
                foreach (var geometry in openEccentricConeCollection)
                {
                    indices.Add(geometry.TreeIndex);
                    indices.Add(GetColorIndex(geometry.Color, headerAttributes.Color));
                    indices.Add(GetFloatIndex(geometry.Diagonal, headerAttributes.Diagonal));
                    indices.Add(GetFloatIndex(geometry.CenterX, headerAttributes.CenterX));
                    indices.Add(GetFloatIndex(geometry.CenterY, headerAttributes.CenterY));
                    indices.Add(GetFloatIndex(geometry.CenterZ, headerAttributes.CenterZ));
                    indices.Add(GetNormalIndex(geometry.CenterAxis, headerAttributes.Normal));
                    indices.Add(GetFloatIndex(geometry.Height, headerAttributes.Height));
                    indices.Add(GetFloatIndex(geometry.RadiusA, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.RadiusB, headerAttributes.Radius));
                    indices.Add(GetNormalIndex(geometry.CapNormal, headerAttributes.Normal));
                }

                var nodeIds = openEccentricConeCollection.Select(b => b.NodeId).ToArray();
                WritePrimitiveCollection(stream, 16, nodeIds, indices.ToArray());
            }

            var openEllipsoidSegmentCollection =
                sectorPrimitiveCollections.OpenEllipsoidSegmentCollection;
            if (openEllipsoidSegmentCollection.Length > 0)
            {
                var indices = new List<ulong>();
                foreach (var geometry in openEllipsoidSegmentCollection)
                {
                    indices.Add(geometry.TreeIndex);
                    indices.Add(GetColorIndex(geometry.Color, headerAttributes.Color));
                    indices.Add(GetFloatIndex(geometry.Diagonal, headerAttributes.Diagonal));
                    indices.Add(GetFloatIndex(geometry.CenterX, headerAttributes.CenterX));
                    indices.Add(GetFloatIndex(geometry.CenterY, headerAttributes.CenterY));
                    indices.Add(GetFloatIndex(geometry.CenterZ, headerAttributes.CenterZ));
                    indices.Add(GetNormalIndex(geometry.Normal, headerAttributes.Normal));
                    indices.Add(GetFloatIndex(geometry.Height, headerAttributes.Height));
                    indices.Add(GetFloatIndex(geometry.HorizontalRadius, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.VerticalRadius, headerAttributes.Radius));
                }

                var nodeIds = openEllipsoidSegmentCollection.Select(b => b.NodeId).ToArray();
                WritePrimitiveCollection(stream, 17, nodeIds, indices.ToArray());
            }

            var openExtrudedRingSegmentCollection =
                sectorPrimitiveCollections.OpenExtrudedRingSegmentCollection;
            if (openExtrudedRingSegmentCollection.Length > 0)
            {
                var indices = new List<ulong>();
                foreach (var geometry in openExtrudedRingSegmentCollection)
                {
                    indices.Add(geometry.TreeIndex);
                    indices.Add(GetColorIndex(geometry.Color, headerAttributes.Color));
                    indices.Add(GetFloatIndex(geometry.Diagonal, headerAttributes.Diagonal));
                    indices.Add(GetFloatIndex(geometry.CenterX, headerAttributes.CenterX));
                    indices.Add(GetFloatIndex(geometry.CenterY, headerAttributes.CenterY));
                    indices.Add(GetFloatIndex(geometry.CenterZ, headerAttributes.CenterZ));
                    indices.Add(GetNormalIndex(geometry.CenterAxis, headerAttributes.Normal));
                    indices.Add(GetFloatIndex(geometry.Height, headerAttributes.Height));
                    indices.Add(GetFloatIndex(geometry.InnerRadius, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.OuterRadius, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.RotationAngle, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.ArcAngle, headerAttributes.Angle));
                }

                var nodeIds = openExtrudedRingSegmentCollection.Select(b => b.NodeId).ToArray();
                WritePrimitiveCollection(stream, 18, nodeIds, indices.ToArray());
            }

            var openSphericalSegmentCollection =
                sectorPrimitiveCollections.OpenSphericalSegmentCollection;
            if (openSphericalSegmentCollection.Length > 0)
            {
                var indices = new List<ulong>();
                foreach (var geometry in openSphericalSegmentCollection)
                {
                    indices.Add(geometry.TreeIndex);
                    indices.Add(GetColorIndex(geometry.Color, headerAttributes.Color));
                    indices.Add(GetFloatIndex(geometry.Diagonal, headerAttributes.Diagonal));
                    indices.Add(GetFloatIndex(geometry.CenterX, headerAttributes.CenterX));
                    indices.Add(GetFloatIndex(geometry.CenterY, headerAttributes.CenterY));
                    indices.Add(GetFloatIndex(geometry.CenterZ, headerAttributes.CenterZ));
                    indices.Add(GetNormalIndex(geometry.Normal, headerAttributes.Normal));
                    indices.Add(GetFloatIndex(geometry.Height, headerAttributes.Height));
                    indices.Add(GetFloatIndex(geometry.Radius, headerAttributes.Radius));
                }

                var nodeIds = openSphericalSegmentCollection.Select(b => b.NodeId).ToArray();
                WritePrimitiveCollection(stream, 20, nodeIds, indices.ToArray());
            }

            var openTorusSegmentCollection = sectorPrimitiveCollections.OpenTorusSegmentCollection;
            if (openTorusSegmentCollection.Length > 0)
            {
                var indices = new List<ulong>();
                foreach (var geometry in openTorusSegmentCollection)
                {
                    indices.Add(geometry.TreeIndex);
                    indices.Add(GetColorIndex(geometry.Color, headerAttributes.Color));
                    indices.Add(GetFloatIndex(geometry.Diagonal, headerAttributes.Diagonal));
                    indices.Add(GetFloatIndex(geometry.CenterX, headerAttributes.CenterX));
                    indices.Add(GetFloatIndex(geometry.CenterY, headerAttributes.CenterY));
                    indices.Add(GetFloatIndex(geometry.CenterZ, headerAttributes.CenterZ));
                    indices.Add(GetNormalIndex(geometry.Normal, headerAttributes.Normal));
                    indices.Add(GetFloatIndex(geometry.Radius, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.TubeRadius, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.RotationAngle, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.ArcAngle, headerAttributes.Angle));
                }

                var nodeIds = openTorusSegmentCollection.Select(b => b.NodeId).ToArray();
                WritePrimitiveCollection(stream, 21, nodeIds, indices.ToArray());
            }

            var ringCollection = sectorPrimitiveCollections.RingCollection;
            if (ringCollection.Length > 0)
            {
                var indices = new List<ulong>();
                foreach (var geometry in ringCollection)
                {
                    indices.Add(geometry.TreeIndex);
                    indices.Add(GetColorIndex(geometry.Color, headerAttributes.Color));
                    indices.Add(GetFloatIndex(geometry.Diagonal, headerAttributes.Diagonal));
                    indices.Add(GetFloatIndex(geometry.CenterX, headerAttributes.CenterX));
                    indices.Add(GetFloatIndex(geometry.CenterY, headerAttributes.CenterY));
                    indices.Add(GetFloatIndex(geometry.CenterZ, headerAttributes.CenterZ));
                    indices.Add(GetNormalIndex(geometry.Normal, headerAttributes.Normal));
                    indices.Add(GetFloatIndex(geometry.InnerRadius, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.OuterRadius, headerAttributes.Radius));
                }

                var nodeIds = ringCollection.Select(b => b.NodeId).ToArray();
                WritePrimitiveCollection(stream, 22, nodeIds, indices.ToArray());
            }

            var sphereCollection = sectorPrimitiveCollections.SphereCollection;
            if (sphereCollection.Length > 0)
            {
                var indices = new List<ulong>();
                foreach (var geometry in sphereCollection)
                {
                    indices.Add(geometry.TreeIndex);
                    indices.Add(GetColorIndex(geometry.Color, headerAttributes.Color));
                    indices.Add(GetFloatIndex(geometry.Diagonal, headerAttributes.Diagonal));
                    indices.Add(GetFloatIndex(geometry.CenterX, headerAttributes.CenterX));
                    indices.Add(GetFloatIndex(geometry.CenterY, headerAttributes.CenterY));
                    indices.Add(GetFloatIndex(geometry.CenterZ, headerAttributes.CenterZ));
                    indices.Add(GetFloatIndex(geometry.Radius, headerAttributes.Radius));
                }

                var nodeIds = sphereCollection.Select(b => b.NodeId).ToArray();
                WritePrimitiveCollection(stream, 23, nodeIds, indices.ToArray());
            }

            var torusCollection = sectorPrimitiveCollections.TorusCollection;
            if (torusCollection.Length > 0)
            {
                var indices = new List<ulong>();
                foreach (var geometry in torusCollection)
                {
                    indices.Add(geometry.TreeIndex);
                    indices.Add(GetColorIndex(geometry.Color, headerAttributes.Color));
                    indices.Add(GetFloatIndex(geometry.Diagonal, headerAttributes.Diagonal));
                    indices.Add(GetFloatIndex(geometry.CenterX, headerAttributes.CenterX));
                    indices.Add(GetFloatIndex(geometry.CenterY, headerAttributes.CenterY));
                    indices.Add(GetFloatIndex(geometry.CenterZ, headerAttributes.CenterZ));
                    indices.Add(GetNormalIndex(geometry.Normal, headerAttributes.Normal));
                    indices.Add(GetFloatIndex(geometry.Radius, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.TubeRadius, headerAttributes.Radius));
                }

                var nodeIds = torusCollection.Select(b => b.NodeId).ToArray();
                WritePrimitiveCollection(stream, 24, nodeIds, indices.ToArray());
            }

            var openGeneralCylinderCollection =
                sectorPrimitiveCollections.OpenGeneralCylinderCollection;
            if (openGeneralCylinderCollection.Length > 0)
            {
                var indices = new List<ulong>();
                foreach (var geometry in openGeneralCylinderCollection)
                {
                    indices.Add(geometry.TreeIndex);
                    indices.Add(GetColorIndex(geometry.Color, headerAttributes.Color));
                    indices.Add(GetFloatIndex(geometry.Diagonal, headerAttributes.Diagonal));
                    indices.Add(GetFloatIndex(geometry.CenterX, headerAttributes.CenterX));
                    indices.Add(GetFloatIndex(geometry.CenterY, headerAttributes.CenterY));
                    indices.Add(GetFloatIndex(geometry.CenterZ, headerAttributes.CenterZ));
                    indices.Add(GetNormalIndex(geometry.CenterAxis, headerAttributes.Normal));
                    indices.Add(GetFloatIndex(geometry.Height, headerAttributes.Height));
                    indices.Add(GetFloatIndex(geometry.Radius, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.RotationAngle, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.ArcAngle, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.SlopeA, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.SlopeB, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.ZangleA, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.ZangleB, headerAttributes.Angle));
                }

                var nodeIds = openGeneralCylinderCollection.Select(b => b.NodeId).ToArray();
                WritePrimitiveCollection(stream, 30, nodeIds, indices.ToArray());
            }

            var closedGeneralCylinderCollection =
                sectorPrimitiveCollections.ClosedGeneralCylinderCollection;
            if (closedGeneralCylinderCollection.Length > 0)
            {
                var indices = new List<ulong>();
                foreach (var geometry in closedGeneralCylinderCollection)
                {
                    indices.Add(geometry.TreeIndex);
                    indices.Add(GetColorIndex(geometry.Color, headerAttributes.Color));
                    indices.Add(GetFloatIndex(geometry.Diagonal, headerAttributes.Diagonal));
                    indices.Add(GetFloatIndex(geometry.CenterX, headerAttributes.CenterX));
                    indices.Add(GetFloatIndex(geometry.CenterY, headerAttributes.CenterY));
                    indices.Add(GetFloatIndex(geometry.CenterZ, headerAttributes.CenterZ));
                    indices.Add(GetNormalIndex(geometry.CenterAxis, headerAttributes.Normal));
                    indices.Add(GetFloatIndex(geometry.Height, headerAttributes.Height));
                    indices.Add(GetFloatIndex(geometry.Radius, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.RotationAngle, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.ArcAngle, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.SlopeA, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.SlopeB, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.ZangleA, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.ZangleB, headerAttributes.Angle));
                }

                var nodeIds = closedGeneralCylinderCollection.Select(b => b.NodeId).ToArray();
                WritePrimitiveCollection(stream, 31, nodeIds, indices.ToArray());
            }

            var solidOpenGeneralCylinderCollection =
                sectorPrimitiveCollections.SolidOpenGeneralCylinderCollection;
            if (solidOpenGeneralCylinderCollection.Length > 0)
            {
                var indices = new List<ulong>();
                foreach (var geometry in solidOpenGeneralCylinderCollection)
                {
                    indices.Add(geometry.TreeIndex);
                    indices.Add(GetColorIndex(geometry.Color, headerAttributes.Color));
                    indices.Add(GetFloatIndex(geometry.Diagonal, headerAttributes.Diagonal));
                    indices.Add(GetFloatIndex(geometry.CenterX, headerAttributes.CenterX));
                    indices.Add(GetFloatIndex(geometry.CenterY, headerAttributes.CenterY));
                    indices.Add(GetFloatIndex(geometry.CenterZ, headerAttributes.CenterZ));
                    indices.Add(GetNormalIndex(geometry.CenterAxis, headerAttributes.Normal));
                    indices.Add(GetFloatIndex(geometry.Height, headerAttributes.Height));
                    indices.Add(GetFloatIndex(geometry.Radius, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.RotationAngle, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.ArcAngle, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.SlopeA, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.SlopeB, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.ZangleA, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.ZangleB, headerAttributes.Angle));
                }

                var nodeIds = solidOpenGeneralCylinderCollection.Select(b => b.NodeId).ToArray();
                WritePrimitiveCollection(stream, 32, nodeIds, indices.ToArray());
            }

            var solidClosedGeneralCylinderCollection =
                sectorPrimitiveCollections.SolidClosedGeneralCylinderCollection;
            if (solidClosedGeneralCylinderCollection.Length > 0)
            {
                var indices = new List<ulong>();
                foreach (var geometry in solidClosedGeneralCylinderCollection)
                {
                    indices.Add(geometry.TreeIndex);
                    indices.Add(GetColorIndex(geometry.Color, headerAttributes.Color));
                    indices.Add(GetFloatIndex(geometry.Diagonal, headerAttributes.Diagonal));
                    indices.Add(GetFloatIndex(geometry.CenterX, headerAttributes.CenterX));
                    indices.Add(GetFloatIndex(geometry.CenterY, headerAttributes.CenterY));
                    indices.Add(GetFloatIndex(geometry.CenterZ, headerAttributes.CenterZ));
                    indices.Add(GetNormalIndex(geometry.CenterAxis, headerAttributes.Normal));
                    indices.Add(GetFloatIndex(geometry.Height, headerAttributes.Height));
                    indices.Add(GetFloatIndex(geometry.Radius, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.RotationAngle, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.ArcAngle, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.SlopeA, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.SlopeB, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.ZangleA, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.ZangleB, headerAttributes.Angle));
                }

                var nodeIds = solidClosedGeneralCylinderCollection.Select(b => b.NodeId).ToArray();
                WritePrimitiveCollection(stream, 33, nodeIds, indices.ToArray());
            }

            var openGeneralConeCollection = sectorPrimitiveCollections.OpenGeneralConeCollection;
            if (openGeneralConeCollection.Length > 0)
            {
                var indices = new List<ulong>();
                foreach (var geometry in openGeneralConeCollection)
                {
                    indices.Add(geometry.TreeIndex);
                    indices.Add(GetColorIndex(geometry.Color, headerAttributes.Color));
                    indices.Add(GetFloatIndex(geometry.Diagonal, headerAttributes.Diagonal));
                    indices.Add(GetFloatIndex(geometry.CenterX, headerAttributes.CenterX));
                    indices.Add(GetFloatIndex(geometry.CenterY, headerAttributes.CenterY));
                    indices.Add(GetFloatIndex(geometry.CenterZ, headerAttributes.CenterZ));
                    indices.Add(GetNormalIndex(geometry.CenterAxis, headerAttributes.Normal));
                    indices.Add(GetFloatIndex(geometry.Height, headerAttributes.Height));
                    indices.Add(GetFloatIndex(geometry.RadiusA, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.RadiusB, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.RotationAngle, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.ArcAngle, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.SlopeA, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.SlopeB, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.ZangleA, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.ZangleB, headerAttributes.Angle));
                }

                var nodeIds = openGeneralConeCollection.Select(b => b.NodeId).ToArray();
                WritePrimitiveCollection(stream, 34, nodeIds, indices.ToArray());
            }

            var closedGeneralConeCollection = sectorPrimitiveCollections.ClosedGeneralConeCollection;
            if (closedGeneralConeCollection.Length > 0)
            {
                var indices = new List<ulong>();
                foreach (var geometry in closedGeneralConeCollection)
                {
                    indices.Add(geometry.TreeIndex);
                    indices.Add(GetColorIndex(geometry.Color, headerAttributes.Color));
                    indices.Add(GetFloatIndex(geometry.Diagonal, headerAttributes.Diagonal));
                    indices.Add(GetFloatIndex(geometry.CenterX, headerAttributes.CenterX));
                    indices.Add(GetFloatIndex(geometry.CenterY, headerAttributes.CenterY));
                    indices.Add(GetFloatIndex(geometry.CenterZ, headerAttributes.CenterZ));
                    indices.Add(GetNormalIndex(geometry.CenterAxis, headerAttributes.Normal));
                    indices.Add(GetFloatIndex(geometry.Height, headerAttributes.Height));
                    indices.Add(GetFloatIndex(geometry.RadiusA, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.RadiusB, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.RotationAngle, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.ArcAngle, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.SlopeA, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.SlopeB, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.ZangleA, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.ZangleB, headerAttributes.Angle));
                }

                var nodeIds = closedGeneralConeCollection.Select(b => b.NodeId).ToArray();
                WritePrimitiveCollection(stream, 35, nodeIds, indices.ToArray());
            }

            var solidOpenGeneralConeCollection =
                sectorPrimitiveCollections.SolidOpenGeneralConeCollection;
            if (solidOpenGeneralConeCollection.Length > 0)
            {
                var indices = new List<ulong>();
                foreach (var geometry in solidOpenGeneralConeCollection)
                {
                    indices.Add(geometry.TreeIndex);
                    indices.Add(GetColorIndex(geometry.Color, headerAttributes.Color));
                    indices.Add(GetFloatIndex(geometry.Diagonal, headerAttributes.Diagonal));
                    indices.Add(GetFloatIndex(geometry.CenterX, headerAttributes.CenterX));
                    indices.Add(GetFloatIndex(geometry.CenterY, headerAttributes.CenterY));
                    indices.Add(GetFloatIndex(geometry.CenterZ, headerAttributes.CenterZ));
                    indices.Add(GetNormalIndex(geometry.CenterAxis, headerAttributes.Normal));
                    indices.Add(GetFloatIndex(geometry.Height, headerAttributes.Height));
                    indices.Add(GetFloatIndex(geometry.RadiusA, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.RadiusB, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.RotationAngle, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.ArcAngle, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.SlopeA, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.SlopeB, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.ZangleA, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.ZangleB, headerAttributes.Angle));
                }

                var nodeIds = solidOpenGeneralConeCollection.Select(b => b.NodeId).ToArray();
                WritePrimitiveCollection(stream, 36, nodeIds, indices.ToArray());
            }

            var solidClosedGeneralConeCollection =
                sectorPrimitiveCollections.SolidClosedGeneralConeCollection;
            if (solidClosedGeneralConeCollection.Length > 0)
            {
                var indices = new List<ulong>();
                foreach (var geometry in solidClosedGeneralConeCollection)
                {
                    indices.Add(geometry.TreeIndex);
                    indices.Add(GetColorIndex(geometry.Color, headerAttributes.Color));
                    indices.Add(GetFloatIndex(geometry.Diagonal, headerAttributes.Diagonal));
                    indices.Add(GetFloatIndex(geometry.CenterX, headerAttributes.CenterX));
                    indices.Add(GetFloatIndex(geometry.CenterY, headerAttributes.CenterY));
                    indices.Add(GetFloatIndex(geometry.CenterZ, headerAttributes.CenterZ));
                    indices.Add(GetNormalIndex(geometry.CenterAxis, headerAttributes.Normal));
                    indices.Add(GetFloatIndex(geometry.Height, headerAttributes.Height));
                    indices.Add(GetFloatIndex(geometry.RadiusA, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.RadiusB, headerAttributes.Radius));
                    indices.Add(GetFloatIndex(geometry.RotationAngle, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.ArcAngle, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.SlopeA, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.SlopeB, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.ZangleA, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.ZangleB, headerAttributes.Angle));
                }

                var nodeIds = solidClosedGeneralConeCollection.Select(b => b.NodeId).ToArray();
                WritePrimitiveCollection(stream, 37, nodeIds, indices.ToArray());
            }

            var triangleMeshCollection = sectorPrimitiveCollections.TriangleMeshCollection;
            if (triangleMeshCollection.Length > 0)
            {
                var indices = new List<ulong>();
                foreach (var geometry in triangleMeshCollection)
                {
                    indices.Add(geometry.TreeIndex);
                    indices.Add(GetUint64Index(geometry.FileId, headerAttributes.FileId));
#pragma warning disable 612
                    indices.Add(GetTextureIndex(geometry.DiffuseTexture, headerAttributes.Texture));
                    indices.Add(GetTextureIndex(geometry.SpecularTexture, headerAttributes.Texture));
                    indices.Add(GetTextureIndex(geometry.AmbientTexture, headerAttributes.Texture));
                    indices.Add(GetTextureIndex(geometry.NormalTexture, headerAttributes.Texture));
                    indices.Add(GetTextureIndex(geometry.BumpTexture, headerAttributes.Texture));
#pragma warning restore 612
                    indices.Add(geometry.TriangleCount);
                    indices.Add(GetColorIndex(geometry.Color, headerAttributes.Color));
                    indices.Add(GetFloatIndex(geometry.Diagonal, headerAttributes.Diagonal));
                }

                var nodeIds = triangleMeshCollection.Select(b => b.NodeId).ToArray();
                WritePrimitiveCollection(stream, 100, nodeIds, indices.ToArray());
            }

            var instancedMeshCollection = sectorPrimitiveCollections.InstancedMeshCollection;
            if (instancedMeshCollection.Length > 0)
            {
                var indices = new List<ulong>();
                foreach (var geometry in instancedMeshCollection)
                {
                    indices.Add(geometry.TreeIndex);
                    indices.Add(GetUint64Index(geometry.FileId, headerAttributes.FileId));
#pragma warning disable 612
                    indices.Add(GetTextureIndex(geometry.DiffuseTexture, headerAttributes.Texture));
                    indices.Add(GetTextureIndex(geometry.SpecularTexture, headerAttributes.Texture));
                    indices.Add(GetTextureIndex(geometry.AmbientTexture, headerAttributes.Texture));
                    indices.Add(GetTextureIndex(geometry.NormalTexture, headerAttributes.Texture));
                    indices.Add(GetTextureIndex(geometry.BumpTexture, headerAttributes.Texture));
#pragma warning enable 612
                    indices.Add(geometry.TriangleOffset);
                    indices.Add(geometry.TriangleCount);
                    indices.Add(GetColorIndex(geometry.Color, headerAttributes.Color));
                    indices.Add(GetFloatIndex(geometry.Diagonal, headerAttributes.Diagonal));
                    indices.Add(GetFloatIndex(geometry.TranslationX, headerAttributes.TranslationX));
                    indices.Add(GetFloatIndex(geometry.TranslationY, headerAttributes.TranslationY));
                    indices.Add(GetFloatIndex(geometry.TranslationZ, headerAttributes.TranslationZ));
                    indices.Add(GetFloatIndex(geometry.RotationX, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.RotationY, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.RotationZ, headerAttributes.Angle));
                    indices.Add(GetFloatIndex(geometry.ScaleX, headerAttributes.ScaleX));
                    indices.Add(GetFloatIndex(geometry.ScaleY, headerAttributes.ScaleY));
                    indices.Add(GetFloatIndex(geometry.ScaleZ, headerAttributes.ScaleZ));
                }

                var nodeIds = instancedMeshCollection.Select(b => b.NodeId).ToArray();
                WritePrimitiveCollection(stream, 101, nodeIds, indices.ToArray());
            }
        }

        private static ulong GetColorIndex(int[] targetColor, int[][] colorAttributeArray)
        {
            for (int i = 0; i < colorAttributeArray.Length; i++)
            {
                var colorAttribute = colorAttributeArray[i];
                if (targetColor.SequenceEqual(colorAttribute))
                {
                    return (ulong)i + 1;
                }
            }

            throw new KeyNotFoundException();
        }

        private static ulong GetTextureIndex(TriangleMesh.Texture targetTexture,
            TriangleMesh.Texture[] textureAttributeArray)
        {
            if (targetTexture.FileId == 0.0 && targetTexture.Width == 0 && targetTexture.Height == 0) { return 0; }
            for (int i = 0; i < textureAttributeArray.Length; i++)
            {
                var attributeTexture = textureAttributeArray[i];
                if (targetTexture.Height == attributeTexture.Height && targetTexture.Width == attributeTexture.Width && targetTexture.FileId == attributeTexture.FileId)
                {
                    return (ulong)i;
                }
            }

            throw new KeyNotFoundException();
        }

        private static ulong GetNormalIndex(float[] targetNormal, float[][] attributeArray)
        {
            for (int i = 0; i < attributeArray.Length; i++)
            {
                var attributeNormal = attributeArray[i];
                if (targetNormal.SequenceEqual(attributeNormal))
                {
                    return (ulong)i;
                }
            }

            throw new KeyNotFoundException();
        }

        private static ulong GetFloatIndex(float targetFloat, float[] attributeArray)
        {
            for (int i = 0; i < attributeArray.Length; i++)
            {
                // TODO: decide on precision here and in Runner
                if (targetFloat == attributeArray[i])
                {
                    return (ulong)i;
                }
            }

            throw new KeyNotFoundException();
        }

        private static ulong GetUint64Index(ulong targetUlong, ulong[] attributeArray)
        {
            for (int i = 0; i < attributeArray.Length; i++)
            {
                if (targetUlong == attributeArray[i])
                {
                    return (ulong)i;
                }
            }

            throw new KeyNotFoundException();
        }

        private static void WritePrimitiveCollection(Stream stream, byte geometryType, ulong[] nodeIds, ulong[] indices)
        {
            var encodedIndices = FibbonaciEncoding.EncodeArray(indices);
            byte attributeCount = (byte)(indices.Length / nodeIds.Length);
            stream.WriteByte(geometryType);
            stream.WriteUint32((uint)nodeIds.Length);
            stream.WriteByte(attributeCount);
            stream.WriteUint32((uint)encodedIndices.Length);
            WriteNodeIds(nodeIds, stream);
            stream.Write(encodedIndices, 0, encodedIndices.Length);
        }

        private static void WriteNodeIds(ulong[] nodeIds, Stream stream)
        {
            foreach (ulong nodeId in nodeIds)
            {
                stream.WriteUint48(nodeId & 0xffffffffffff);
                stream.WriteByte((byte)((nodeId >> 48) & 0xff));
            }
        }

        private static void WriteHeader(Header sectorHeader, Stream stream)
        {
            stream.WriteUint32(sectorHeader.MagicBytes);
            stream.WriteUint32(sectorHeader.FormatVersion);
            stream.WriteUint32(sectorHeader.OptimizerVersion);
            stream.WriteUint64(sectorHeader.SectorId);
            ulong parentSectorId = (ulong?)sectorHeader.ParentSectorId ?? ulong.MaxValue;
            stream.WriteUint64(parentSectorId);
            stream.WriteFloat(sectorHeader.BboxMin[0]);
            stream.WriteFloat(sectorHeader.BboxMin[1]);
            stream.WriteFloat(sectorHeader.BboxMin[2]);
            stream.WriteFloat(sectorHeader.BboxMax[0]);
            stream.WriteFloat(sectorHeader.BboxMax[1]);
            stream.WriteFloat(sectorHeader.BboxMax[2]);

            stream.WriteUint32(AttributeCount);

            if (sectorHeader.Attributes != null)
            {
                stream.WriteRgbaArray(sectorHeader.Attributes.Color);
                stream.WriteFloatArray(sectorHeader.Attributes.Diagonal);
                stream.WriteFloatArray(sectorHeader.Attributes.CenterX);
                stream.WriteFloatArray(sectorHeader.Attributes.CenterY);
                stream.WriteFloatArray(sectorHeader.Attributes.CenterZ);
                stream.WriteNormalArray(sectorHeader.Attributes.Normal);
                stream.WriteFloatArray(sectorHeader.Attributes.Delta);
                stream.WriteFloatArray(sectorHeader.Attributes.Height);
                stream.WriteFloatArray(sectorHeader.Attributes.Radius);
                stream.WriteFloatArray(sectorHeader.Attributes.Angle);
                stream.WriteFloatArray(sectorHeader.Attributes.TranslationX);
                stream.WriteFloatArray(sectorHeader.Attributes.TranslationY);
                stream.WriteFloatArray(sectorHeader.Attributes.TranslationZ);
                stream.WriteFloatArray(sectorHeader.Attributes.ScaleX);
                stream.WriteFloatArray(sectorHeader.Attributes.ScaleY);
                stream.WriteFloatArray(sectorHeader.Attributes.ScaleZ);
                stream.WriteUint64Array(sectorHeader.Attributes.FileId);
                stream.WriteTextureArray(sectorHeader.Attributes.Texture);
            }
        }
    }
}