namespace CadRevealFbxProvider.Attributes
{
    using System;

    /// <summary>
    /// Exception thrown when an error occurs when the scaffolding filename is not according to the specification.
    /// </summary>
    public class ScaffoldingFilenameException : Exception
    {
        public ScaffoldingFilenameException(string message)
            : base(message) { }

        public ScaffoldingFilenameException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
