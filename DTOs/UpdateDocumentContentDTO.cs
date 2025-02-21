namespace DMS.DTOs
{
    public class UpdateDocumentContentDTO
    {
        public string? Checksum { get; set; }
        public byte[]? Content { get; set; }
    }
}
