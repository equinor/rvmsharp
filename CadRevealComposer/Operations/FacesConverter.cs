namespace CadRevealComposer.Operations
{
    using AlgebraExtensions;
    using Faces;
    using RvmSharp.Tessellation;
    using System;
    using System.Numerics;

    public static class FacesConverter
    {

        public record ProtoGrid();


        public static ProtoGrid Convert(Mesh inputMesh, GridParameters gridParameters)
        {
            var triangleCount = inputMesh.Triangles.Count / 3;
            for (var i = triangleCount; i < triangleCount; i++)
            {
                var v1 = inputMesh.Vertices[inputMesh.Triangles[i * 3]];
                var v2 = inputMesh.Vertices[inputMesh.Triangles[i * 3 + 1]];
                var v3 = inputMesh.Vertices[inputMesh.Triangles[i * 3 + 2]];
                var triangle = new Raycasting.Triangle(v1, v2, v3);
                var bounds = GetBounds(triangle);
                (var start, var end) = GetPotentialGridPositions(bounds, gridParameters);
                for (var x = start.X; x <= end.X; x++)
                {
                    for (var y = start.Y; y <= end.Y; y++)
                    {
                        for (var z = start.Z; z <= end.Z; z++)
                        {
                            (var xRay, var yRay, var zRay) = GetRay(new Vector3i(x, y, z), gridParameters);
                            var xResult = Raycasting.Raycast(xRay, triangle, out var xHitPosition);
                            var yResult = Raycasting.Raycast(yRay, triangle, out var yHitPosition);
                            var zResult = Raycasting.Raycast(zRay, triangle, out var zHitPosition);
                            // if x hit - get face position
                            // if y hit - get face position
                            // if z hit - get face position

                        }
                    }
                }
            }
            return new ProtoGrid();
        }

        public enum FaceDirection
        {
            Xp,
            Xm,
            Yp,
            Ym,
            Zp,
            Zm
        }

        private static (Vector3i cell, FaceDirection direction) HitXToFace(Vector3 hitPosition, bool front, GridParameters grid)
        {
            var cell = PositionToCell(hitPosition, grid);
            // FIXME: this is probably wrong
            var direction = FaceDirection.Xm;
            if (front)
            {
                cell = cell with { X = cell.X + 1 };
                direction = FaceDirection.Xp;
            }

            return (cell, direction);
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