﻿namespace DMS.DTOs
{
    public class UpdateDocumentMetadataDTO
    {
        public string? FileName { get; set; }
        public bool? IsPublic { get; set; }

        public List<string> Tags { get; set; }
    }
}
