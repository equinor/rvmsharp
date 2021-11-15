namespace CadRevealComposer.Operations
{
    using Faces;
    using RvmSharp.Tessellation;
    using System.Linq;

    public static class FacesConverter
    {

        public record ProtoGrid();


        public static ProtoGrid Convert(Mesh inputMesh, GridParameters gridParameters)
        {
            var triangleCount = inputMesh.Triangles.Count / 3;
            //for (var i = triangleCount; i < triangleCount; i++)

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

    }
}