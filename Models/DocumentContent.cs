namespace DMS.Models
{
    public class DocumentContent
    {
        public int Id { get; set; }

        public int DocumentMetadataId { get; set; }

        public string? Checksum { get; set; }
        public byte[]? Content { get; set; }

        public DocumentMetadata? Metadata { get; set; }
    }
}
