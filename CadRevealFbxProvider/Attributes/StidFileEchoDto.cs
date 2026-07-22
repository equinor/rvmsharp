namespace CadRevealFbxProvider.Attributes
{
    public record StidFileEchoDto
    {
        public required string BlobId { get; set; }
        public required string FileName { get; set; }
    }
}
