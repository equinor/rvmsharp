namespace CadRevealComposer.Utils
{
    using Newtonsoft.Json;
    using System;
    using System.Drawing;
    using System.Numerics;

    public class VisData
    {
        [JsonProperty("meshes")] public Mesh[] Meshes = Array.Empty<Mesh>();

        [JsonProperty("arrows")] public Arrow[] Arrows = Array.Empty<Arrow>();

        [JsonProperty("axes")] public Axe[] Axes = Array.Empty<Axe>();

        public class Arrow
        {
            public Arrow() { }

            public Arrow(Color color, Vector3 origin, Vector3 direction)
            {
                Color = ColorTranslator.ToHtml(color);
                Origin = origin.CopyToNewArray();
                Direction = Vector3.Normalize(direction).CopyToNewArray();
                Length = direction.Length();
            }

            [JsonProperty("color")] public string Color { get; set; }

            [JsonProperty("origin")] public float[] Origin { get; set; }

            [JsonProperty("direction")] public float[] Direction { get; set; }

            [JsonProperty("length")] public float Length { get; set; }
        }

        public class Axe
        {
            [JsonProperty("size")] public float Size { get; set; }

            [JsonProperty("position")] public float[] Position { get; set; }

            [JsonProperty("rotation")] public float[] Rotation { get; set; }
        }

        public class Mesh
        {
            [JsonProperty("color")] public string Color { get; set; }

            [JsonProperty("indices")] public int[] Indices { get; set; }

            [JsonProperty("vertices")] public float[] Vertices { get; set; }

            [JsonProperty("normals")] public float[] Normals { get; set; }
        }
    }
}