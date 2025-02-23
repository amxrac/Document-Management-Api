using DMS.Data;
using DMS.Models;
using DMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.Xml.Linq;
using DMS.DTOs;
using static iText.Svg.SvgConstants;

namespace DMS.Controllers
{

    [ApiController]
    [Route("api/documents")]
    public class DocumentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly TokenGenerator _tokenGenerator;


        public DocumentsController(AppDbContext context, TokenGenerator tokenGenerator)
        {
            _context = context;
            _tokenGenerator = tokenGenerator;
        }

        private bool IsValidFileType(byte[] fileBytes, out string detectedType)
        {
            detectedType = "Unknown";
            if (fileBytes.Length < 4)
                return false;

            if (fileBytes[0] == 0x25 && fileBytes[1] == 0x50 && fileBytes[2] == 0x44 && fileBytes[3] == 0x46)
            {
                detectedType = "PDF";
                return true;
            }

            if (fileBytes[0] == 0x50 && fileBytes[1] == 0x4B && fileBytes[2] == 0x03 && fileBytes[3] == 0x04)
            {
                detectedType = "DOCX";
                return true;
            }

            return false;
        }

        public string GetMimeType(string extension)
        {
            var types = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { ".pdf", "application/pdf" },
                { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" }
        
            };

            return types.TryGetValue(extension, out var mimeType) ? mimeType : "application/octet-stream";
        }

        [Authorize(Roles = "Admin,Editor")]
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] bool isPublic = false, [FromForm] List<string> tags = null)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Console.WriteLine($"this is the id {userId}");

                if (string.IsNullOrEmpty(userId))
                {

                    return Unauthorized();

                }

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "No file was uploaded." });
                }

                if (file.Length > 5 * 1024 * 1024)
                {
                    return BadRequest(new { message = "Maximum file size is 5MB." });
                }
                
                byte[] fileBytes;
                string checksum;
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    fileBytes = ms.ToArray();
                }

                bool isValidFile = IsValidFileType(fileBytes, out string detectedType);
                if (!isValidFile)
                {
                    return BadRequest(new { message = $"Invalid file type detected: {detectedType}" });
                }

                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    byte[] hash = sha256.ComputeHash(fileBytes);
                    checksum = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }

                string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var documentMetadata = new DocumentMetadata
                {
                    FileName = null,
                    UserId = userId,
                    IsPublic = isPublic,
                    MimeType = GetMimeType(extension),
                    FileSize = fileBytes.Length,
                    DocumentContent = new DocumentContent
                    {
                        Checksum = checksum,
                        Content = fileBytes
                    }

                };

                _context.DocumentMetadata.Add(documentMetadata);
                await _context.SaveChangesAsync();


                documentMetadata.FileName = documentMetadata.Id + extension;
                await _context.SaveChangesAsync();

                if (tags != null && tags.Any())
                {
                    var existingTags = await _context.Tags.Where(t => tags.Contains(t.Name)).ToListAsync();
                    var newTags = tags.Except(existingTags.Select(t => t.Name)).Select(t => new Tag { Name = t }).ToList();

                    _context.Tags.AddRange(newTags);
                    await _context.SaveChangesAsync();

                    var allTags = existingTags.Concat(newTags).ToList();
                    var documentTags = allTags.Select(tag => new DocumentTag
                    {
                        DocumentMetadataId = documentMetadata.Id,
                        TagId = tag.Id
                    }).ToList();

                    _context.DocumentTags.AddRange(documentTags);
                    await _context.SaveChangesAsync();
                }


                var auditLog = new AuditLog
                {
                    Action = "Upload",
                    UserId = userId,
                    DocumentId = documentMetadata.Id
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    id = documentMetadata.Id,
                    fileName = documentMetadata.FileName,
                    fileSize = documentMetadata.FileSize,
                    mimeType = documentMetadata.MimeType,
                    IsPublic = isPublic,
                    createdDate = documentMetadata.CreatedDate,
                    tags = tags ?? new List<string>()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred while uploading the file. {ex.Message}" });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDocumentMetadata(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "Invalid id." });
            }

            var documentMetadata = await _context.DocumentMetadata.AsNoTracking().Include(d => d.DocumentTags)
                .ThenInclude(dt => dt.Tag).FirstOrDefaultAsync(d => d.Id == id);

            if (documentMetadata == null)
            {
                return NotFound(new { message = "Document not found." });
            }

            if (!documentMetadata.IsPublic && !User.Identity.IsAuthenticated)
            {
                return Unauthorized(new { message = "You are not authorized to access this document." });
            }

            return Ok(new
            {
                id = documentMetadata.Id,
                fileName = documentMetadata.FileName,
                fileSize = documentMetadata.FileSize,
                mimeType = documentMetadata.MimeType,
                IsPublic = documentMetadata.IsPublic,
                createdDate = documentMetadata.CreatedDate,
                tags = documentMetadata.DocumentTags?.Select(dt => dt.Tag.Name).ToList()
            });
        }

        [HttpGet("{id:int}/download")]
        public async Task<IActionResult> GetDocument(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "Invalid id." });
            }

            var document = await _context.DocumentMetadata.Include(d => d.DocumentContent).AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);

            if (document == null)
            {
                return NotFound(new { message = "Document not found." });
            }

            if (!document.IsPublic && !User.Identity.IsAuthenticated)
            {
                return Unauthorized(new { message = "You are not authorized to access this document." });
            }

            if (document.DocumentContent?.Content == null)
            {
                return NotFound(new { message = "Document not found." });
            }

            return File(document.DocumentContent.Content,
                        document.MimeType ?? "application/octet-stream",
                        document.FileName ?? "downloaded_file");
        }

        [Authorize(Roles = "Admin,Editor")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateDocumentMetadata(int id, [FromBody] UpdateDocumentMetadataDTO metadataDTO)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "Invalid id." });
            }

            var documentMetadata = await _context.DocumentMetadata.Include(d => d.DocumentTags)
                .ThenInclude(dt => dt.Tag).FirstOrDefaultAsync(d => d.Id == id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);


            if (documentMetadata == null)
            {
                return NotFound(new { message = "Document not found." });
            }

            if (!string.IsNullOrEmpty(metadataDTO.FileName))
            {
                documentMetadata.FileName = metadataDTO.FileName;
            }

            if (metadataDTO.IsPublic.HasValue)
            {
                documentMetadata.IsPublic = metadataDTO.IsPublic.Value;
            }

            if (metadataDTO.Tags.Any())
            {
                var existingTags = await _context.Tags
                    .Where(t => metadataDTO.Tags.Contains(t.Name))
                    .ToListAsync();

                var existingTagIds = documentMetadata.DocumentTags.Select(dt => dt.TagId).ToHashSet();

                var newTags = metadataDTO.Tags
                    .Except(existingTags.Select(t => t.Name))
                    .Select(tagName => new Tag { Name = tagName })
                    .ToList();

                if (newTags.Any())
                {
                    _context.Tags.AddRange(newTags);
                    await _context.SaveChangesAsync();
                }

                var allTags = existingTags.Concat(newTags).ToList();

                var newDocumentTags = allTags
                    .Where(tag => !existingTagIds.Contains(tag.Id)) // Skip already linked tags
                    .Select(tag => new DocumentTag
                    {
                        DocumentMetadataId = documentMetadata.Id,
                        TagId = tag.Id
                    })
                    .ToList();

                _context.DocumentTags.AddRange(newDocumentTags);
                await _context.SaveChangesAsync();
            }

            documentMetadata.LastModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var auditLog = new AuditLog
            {
                Action = "Edit Document Metadata",
                UserId = userId,
                DocumentId = documentMetadata.Id
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = documentMetadata.Id,
                fileName = documentMetadata.FileName,
                fileSize = documentMetadata.FileSize,
                mimeType = documentMetadata.MimeType,
                IsPublic = documentMetadata.IsPublic,
                createdDate = documentMetadata.CreatedDate,
                lastModifiedDate = documentMetadata.LastModifiedDate,
                tags = metadataDTO.Tags
            });
        }

        [Authorize(Roles = "Admin,Editor")]
        [HttpPut("{id:int}/upload")] 
        public async Task<IActionResult> UpdateDocumentContent(int id, IFormFile file)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "Invalid id." });
            }

            var documentMetadata = await _context.DocumentMetadata.FirstOrDefaultAsync(d => d.Id == id);
            var documentContent = await _context.DocumentContent.FirstOrDefaultAsync(d => d.Id == id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (documentMetadata == null)
            {
                return NotFound(new { message = "Document metadata not found." });
            }


            if (documentContent == null)
            {
                return NotFound(new { message = "Document not found." });
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file was uploaded." });
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { message = "Maximum file size is 5MB." });
            }

            byte[] fileBytes;
            string checksum;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                fileBytes = ms.ToArray();
            }

            bool isValidFile = IsValidFileType(fileBytes, out string detectedType);
            if (!isValidFile)
            {
                return BadRequest(new { message = $"Invalid file type detected: {detectedType}" });
            }

            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(fileBytes);
                checksum = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            documentMetadata.FileName = documentMetadata.Id + extension;
            documentMetadata.UserId = userId;
            documentMetadata.MimeType = GetMimeType(extension);
            documentMetadata.FileSize = fileBytes.Length;
            documentMetadata.LastModifiedDate = DateTime.UtcNow;


            documentContent.Checksum = checksum;
            documentContent.Content = fileBytes;



            var auditLog = new AuditLog
            {
                Action = "Edit File Content",
                UserId = userId,
                DocumentId = documentMetadata.Id
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = documentMetadata.Id,
                fileName = documentMetadata.FileName,
                fileSize = documentMetadata.FileSize,
                mimeType = documentMetadata.MimeType,
                IsPublic = documentMetadata.IsPublic,
                createdDate = documentMetadata.CreatedDate
            });
        }

    }
}
