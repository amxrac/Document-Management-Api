namespace DMS.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string Action { get; set; }
        public string UserId { get; set; }
        public int DocumentId { get; set; }
    }
}
