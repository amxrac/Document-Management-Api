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


        [Authorize(Roles = "Admin,Editor")]
        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] bool isPublic = false)
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

                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(fileBytes);
                    checksum = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }

                string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var documentMetadata = new DocumentMetadata
                {
                    FileName = null,
                    UserId = userId,
                    IsPublic = isPublic,
                    MimeType = extension,
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
                    createdDate = documentMetadata.CreatedDate
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

            var documentMetadata = await _context.DocumentMetadata.FirstOrDefaultAsync(d => d.Id == id);

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
                createdDate = documentMetadata.CreatedDate
            });
        }
    }
}
