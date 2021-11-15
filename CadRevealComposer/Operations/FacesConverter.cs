namespace CadRevealComposer.Operations
{
    using AlgebraExtensions;
    using Faces;
    using RvmSharp.Tessellation;
    using System.Collections.Generic;
    using System.Linq;
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
                var potentialGridPositions = GetPotentialGridPositions(bounds, gridParameters);
                foreach (var potentialGridPosition in potentialGridPositions)
                {
                    var ray = GetRay(potentialGridPosition);
                    var result = Raycasting.Raycast(ray, triangle, out var hitPosition);
                    if (result)
                    {
                        var gridFace = GetGridFace(hitPosition, gridParameters);
                        ProtoGrid.AddGridFace(gridFace);
                    }
                }
            }

            // Pseudo code
            // - for each triangle
            // -    get bounds
            // -    define squares to raycast from
            // -    ray-casting
            // -    write results into protogrid


            // Separate protogrid per triangle, merged at the end
            //
            return new ProtoGrid();
        }

        private static Vector3i[] GetPotentialGridPositions(Raycasting.Bounds bounds, GridParameters gridParameters)
        {
            var result = new List<Vector3i>();
            var startX = (bounds.Min - (gridParameters.GridOrigin - Vector3.One * gridParameters.GridIncrement / 2)) /
                         gridParameters.GridIncrement;
            return result.ToArray();
        }

        private static Raycasting.Bounds GetBounds(Raycasting.Triangle triangle)
        {
            var min = Vector3.Min(triangle.V1, Vector3.Min(triangle.V2, triangle.V3));
            var max = Vector3.Max(triangle.V1, Vector3.Max(triangle.V2, triangle.V3));
            return new Raycasting.Bounds(min, max);
        }

    }
}