namespace CadRevealComposer.Operations
{
    using AlgebraExtensions;
    using Faces;
    using RvmSharp.Tessellation;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Numerics;

    public static class FacesConverter
    {

        public record ProtoGrid(GridParameters GridParameters, IReadOnlyDictionary<Vector3i, FaceDirection> Faces);

        public record ProtoFace(FaceDirection FaceDirection, Color FaceColor);

        [Flags]
        public enum FaceDirection
        {
            None = 0,
            Xp = 0b000001,
            Xm = 0b000010,
            Yp = 0b000100,
            Ym = 0b001000,
            Zp = 0b010000,
            Zm = 0b100000,
        }


        public static ProtoGrid Convert(Mesh inputMesh, GridParameters gridParameters)
        {
            var triangleCount = inputMesh.Triangles.Count / 3;
            var faces = new Dictionary<Vector3i, FaceDirection>();
            for (var i = triangleCount; i < triangleCount; i++)
            {
                var v1 = inputMesh.Vertices[inputMesh.Triangles[i * 3]];
                var v2 = inputMesh.Vertices[inputMesh.Triangles[i * 3 + 1]];
                var v3 = inputMesh.Vertices[inputMesh.Triangles[i * 3 + 2]];
                var triangle = new Raycasting.Triangle(v1, v2, v3);
                var bounds = GetBounds(triangle);
                var (start, end) = GetPotentialGridPositions(bounds, gridParameters);
                for (var x = start.X; x <= end.X; x++)
                {
                    for (var y = start.Y; y <= end.Y; y++)
                    {
                        for (var z = start.Z; z <= end.Z; z++)
                        {
                            var (xRay, yRay, zRay) = GetRay(new Vector3i(x, y, z), gridParameters);
                            var xResult = Raycasting.Raycast(xRay, triangle, out var xHitPosition, out var xFrontFace);
                            var yResult = Raycasting.Raycast(yRay, triangle, out var yHitPosition, out var yFrontFace);
                            var zResult = Raycasting.Raycast(zRay, triangle, out var zHitPosition, out var zFrontFace);
                            // if x hit - get face position
                            if (xResult)
                            {
                                (var cell, FaceDirection direction) = HitResultToFaceIn(xHitPosition, xFrontFace, gridParameters, Axis.X);
                                faces[cell] = faces.ContainsKey(cell) ? faces[cell] : FaceDirection.None | direction;
                            }
                            // if y hit - get face position
                            if (yResult)
                            {
                                (var cell, FaceDirection direction) = HitResultToFaceIn(yHitPosition, yFrontFace, gridParameters, Axis.Y);
                                faces[cell] = faces.ContainsKey(cell) ? faces[cell] : FaceDirection.None | direction;
                            }
                            // if z hit - get face position
                            if (zResult)
                            {
                                (var cell, FaceDirection direction) = HitResultToFaceIn(zHitPosition, zFrontFace, gridParameters, Axis.Z);
                                faces[cell] = faces.ContainsKey(cell) ? faces[cell] : FaceDirection.None | direction;
                            }
                        }
                    }
                }
            }
            return new ProtoGrid(gridParameters, faces);
        }

        public static ProtoGrid MergeCells(ProtoGrid input)
        {
            // TODO:
            return input;
        }

        public enum Axis
        {
            X = 0,
            Y = 1,
            Z = 2
        }

        private static (Vector3i cell, FaceDirection direction) HitResultToFaceIn(Vector3 hitPosition, bool isFrontFace, GridParameters grid, Axis axis)
        {
            var cell = PositionToCell(hitPosition, grid);
            var center = grid.GridOrigin + cell * grid.GridIncrement;
            var isHigh = new[]{center.X < hitPosition.X,center.Y < hitPosition.Y,center.Z < hitPosition.Z};
            var lowFaces = new[] { FaceDirection.Xm, FaceDirection.Ym, FaceDirection.Zm };
            var highFaces = new[] { FaceDirection.Xp, FaceDirection.Yp, FaceDirection.Zp };
            var axisIndex = (int)axis;
            var direction = isFrontFace ? lowFaces[axisIndex] : highFaces[axisIndex];


            if (isFrontFace & isHigh[axisIndex])
            {
                cell[axisIndex]++;
            } else if (!isFrontFace & !isHigh[axisIndex])
            {
                cell[axisIndex]--;
            }

            return (cell, direction);
        }

        private static Vector3i PositionToCellWall(Vector3 pos, GridParameters grid)
        {
            return PositionToCell(pos,
                grid with { GridOrigin = grid.GridOrigin - Vector3.One * grid.GridIncrement / 2 });
        }


        private static (Raycasting.Ray xRay, Raycasting.Ray yRay, Raycasting.Ray zRay) GetRay(Vector3i target, GridParameters grid)
        {
            var newTarget = grid.GridOrigin + Vector3.One * new Vector3(target.X, target.Y, target.Z) * grid.GridIncrement;
            var newOrigin = grid.GridOrigin - grid.GridIncrement * Vector3.One;
            var xRay = new Raycasting.Ray(new Vector3(newOrigin.X, newTarget.Y, newTarget.Z), Vector3.UnitX);
            var yRay = new Raycasting.Ray(new Vector3(newTarget.X, newOrigin.Y, newTarget.Z), Vector3.UnitY);
            var zRay = new Raycasting.Ray(new Vector3(newTarget.X, newTarget.Y, newOrigin.Z), Vector3.UnitZ);
            return (xRay, yRay, zRay);
        }

        private static (Vector3i start, Vector3i end) GetPotentialGridPositions(Raycasting.Bounds bounds, GridParameters gridParameters)
        {
            var start = PositionToCell(bounds.Min, gridParameters);
            var end = PositionToCell(bounds.Max, gridParameters);
            return (start, end);
        }

        private static Vector3i PositionToCell(Vector3 position, GridParameters grid)
        {
            var startF = (position - (grid.GridOrigin - Vector3.One * grid.GridIncrement / 2)) /
                         grid.GridIncrement;
            return new Vector3i((int)MathF.Floor(startF.X), (int)MathF.Floor(startF.Y), (int)MathF.Floor(startF.Z));
        }

        private static Raycasting.Bounds GetBounds(Raycasting.Triangle triangle)
        {
            var min = Vector3.Min(triangle.V1, Vector3.Min(triangle.V2, triangle.V3));
            var max = Vector3.Max(triangle.V1, Vector3.Max(triangle.V2, triangle.V3));
            return new Raycasting.Bounds(min, max);
        }
    }
}