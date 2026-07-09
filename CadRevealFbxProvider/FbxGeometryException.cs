namespace CadRevealFbxProvider
{
    using System;

    /// <summary>
    /// Exception thrown when an error occurs when the scaffolding filename is not according to the specification.
    /// </summary>
    public class FbxGeometryException : Exception
    {
        public FbxGeometryException(string message)
            : base(message) { }

        public FbxGeometryException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
