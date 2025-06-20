namespace CadRevealFbxProvider.Attributes
{
    using System;

    /// <summary>
    /// Exception thrown when an error occurs during attribute parsing of a scaffolding model.
    /// </summary>
    public class ScaffoldingMetadataMissingFieldException : Exception
    {
        public ScaffoldingMetadataMissingFieldException(string message)
            : base(message) { }

        public ScaffoldingMetadataMissingFieldException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
