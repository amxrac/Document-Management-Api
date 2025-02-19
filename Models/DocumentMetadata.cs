namespace DMS.Models
{
    public class DocumentMetadata
    {
        public int Id { get; set; }
        public string? FileName { get; set; }
        public string UserId { get; set; }
        public bool IsPublic { get; set; } = false;  // false - only admin and user can access it. true - anyone can access it via the endpoint with its id
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastModifiedDate { get; set;} = DateTime.Now;
        public string? MimeType { get; set; }
        public long FileSize { get; set; }
        public List<DocumentTag>? DocumentTags { get; set; }
        public DocumentContent? DocumentContent { get; set; }

    }
}
