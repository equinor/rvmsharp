namespace CadRevealComposer.Primitives
{
    [System.AttributeUsage(
        validOn: System.AttributeTargets.Field | System.AttributeTargets.Property,
        AllowMultiple = false,
        Inherited = true)]
    public class I3dfAttribute : System.Attribute
    {
        public readonly AttributeType Kind;

        public I3dfAttribute(AttributeType kind)
        {
            Kind = kind;
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
            Texture,
            Ignore
        }
    }
}