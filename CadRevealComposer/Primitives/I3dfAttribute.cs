namespace CadRevealComposer.Primitives
{
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property)]
    public class I3dfAttribute : System.Attribute
    {
        public readonly AttributeType Type;

        public I3dfAttribute(AttributeType type)
        {
            Type = type;
        }
        
        public enum AttributeType
        {
            Null,
            Color,
            Diagonal,
            CenterX,
            CenterY,
            CenterZ,
            Normal,
            Delta,
            Height,
            Radius,
            Angle,
            TranslationX,
            TranslationY,
            TranslationZ,
            ScaleX,
            ScaleY,
            ScaleZ,
            FileId,
            Texture
        }
    }
}