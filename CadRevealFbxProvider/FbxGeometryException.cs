namespace CadRevealFbxProvider
{
    using System;

    /// <summary>
    /// Exception thrown when there is an issue with the FBX file or its content.
    /// </summary>
    public class FbxGeometryException : Exception
    {
        public FbxGeometryException(string message)
            : base(message) { }

        public FbxGeometryException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
