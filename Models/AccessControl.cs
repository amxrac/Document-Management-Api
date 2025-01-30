namespace DMS.Models
{
    public class AccessControl
    {
        public int DocumentMetadataId { get; set; }
        public int UserId { get; set; }
        public string? Permission { get; set; }
    }
}
