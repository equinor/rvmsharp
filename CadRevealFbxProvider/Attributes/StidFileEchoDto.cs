namespace CadRevealFbxProvider.Attributes
{
    /// This is a copy of the record with the same name from EchoModelDistribution project, which exports the json file
    public record StidFileEchoDto
    {
        public required string BlobId { get; set; }
        public required string FileName { get; set; }
    }
}
