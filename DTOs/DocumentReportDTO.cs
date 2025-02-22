namespace DMS.DTOs
{
    public class DocumentReportDTO
    {
        public int DocumentId { get; set; }
        public string FileName { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public long FileSize { get; set; }
        public bool IsPublic { get; set; }
        public string CreatedBy { get; set; }
        public string Email { get; set; }
    }
}
