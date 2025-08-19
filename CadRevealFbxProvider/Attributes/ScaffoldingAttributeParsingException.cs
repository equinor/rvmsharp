namespace CadRevealFbxProvider.Attributes
{
    using System;

    /// <summary>
    /// Exception thrown when an error occurs when something unexpected or wrong occurs during parsing of scaffolding attributes.
    /// </summary>
    public class ScaffoldingAttributeParsingException : Exception
    {
        public ScaffoldingAttributeParsingException(string message)
            : base(message) { }

        public ScaffoldingAttributeParsingException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
