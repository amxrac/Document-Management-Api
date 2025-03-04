﻿namespace DMS.Models
{
    public class DocumentTag
    {
        public int Id { get; set; }
        public int DocumentMetadataId { get; set; }
        public int TagId { get; set; }
        public Tag? Tag { get; set; }
        public DocumentMetadata? DocumentMetadata { get; set; }
    }
}
