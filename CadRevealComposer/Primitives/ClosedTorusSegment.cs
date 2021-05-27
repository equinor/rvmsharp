namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public class ClosedTorusSegment : APrimitive
    {
        [JsonProperty("color")]
        public int[] Color { get; set; }

        [JsonProperty("diagonal")]
        public float Diagonal { get; set; }

        [JsonProperty("center_x")]
        public float CenterX { get; set; }

        [JsonProperty("center_y")]
        public float CenterY { get; set; }

        [JsonProperty("center_z")]
        public float CenterZ { get; set; }

        [JsonProperty("normal")]
        public float[] Normal { get; set; }

        [JsonProperty("radius")]
        public float Radius { get; set; }

        [JsonProperty("tube_radius")]
        public float TubeRadius { get; set; }
        
        [JsonProperty("rotation_angle")]
        public float RotationAngle { get; set; }
        
        [JsonProperty("arc_angle")]
        public float ArcAngle { get; set; }
    }
}