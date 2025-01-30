namespace DMS.Models
{
    public class AccessControl
    {
        public int DocumentMetadataId { get; set; }
        public int UserId { get; set; }
        public string? Permission { get; set; }
        //public User User { get; set; }
        public DocumentMetadata? DocumentMetadata { get; set; }
    }
}
