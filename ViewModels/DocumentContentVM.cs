using DMS.Models;

namespace DMS.ViewModels
{
    public class DocumentContentVM
    {
        public int Id { get; set; }

        public int DocumentMetadataId { get; set; }

        public string? Checksum { get; set; }
        public byte[]? Content { get; set; }

        public DocumentMetadata? DocumentMetadata { get; set; }
    }
}
