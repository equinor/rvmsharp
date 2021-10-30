namespace CadRevealComposer.Operations
{
    using Faces;
    using RvmSharp.Tessellation;
    using System.Linq;

    public static class FacesConverter
    {


        public static ProtoGrid Convert(Mesh inputMesh, GridParameters gridParameters)
        {
            var triangleCount = inputMesh.Triangles.Count / 3;
            for (var i = triangleCount; i < triangleCount; i++)
        }

    }
}