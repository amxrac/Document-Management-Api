using DMS.Data;
using DMS.DTOs;
using DMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Geom;
using iText.Layout.Properties;
using iText.Kernel.Colors;


namespace DMS.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api")]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public ReportsController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var usersList = await _context.Users.ToListAsync();

            var usersDto = new List<UserDTO>();

            foreach (var user in usersList)
            {
                var roles = await _userManager.GetRolesAsync(user);
                usersDto.Add(new UserDTO
                {
                    Id = user.Id,
                    Email = user.Email,
                    Role = roles.FirstOrDefault() 
                });
            }

            return Ok(usersDto);
        }

        private async Task<List<DocumentReportDTO>> GetDocumentReportData()
        {
            return await _context.DocumentMetadata.AsNoTracking()
                .Include(d => d.User)
                .Select(d => new DocumentReportDTO
                {
                    DocumentId = d.Id,
                    FileName = d.FileName,
                    CreatedDate = d.CreatedDate,
                    LastModifiedDate = d.LastModifiedDate,
                    FileSize = d.FileSize,
                    IsPublic = d.IsPublic,
                    CreatedBy = d.User.UserName,
                    Email = d.User.Email,
                    Tags = d.DocumentTags.Select(dt => dt.Tag.Name).ToList()
                })
                .ToListAsync();
        }

        [HttpGet("reports")]
        public async Task<IActionResult> GetReport([FromQuery] string format = "json")
        {
            var reportData = await GetDocumentReportData();

            switch (format.ToLower())
            {
                case "excel":
                    return await GenerateExcelReport(reportData);
                case "pdf":
                    return await GeneratePdfReport(reportData);
                case "json":
                default:
                    return Ok(reportData);
            }
        }

        private async Task<FileContentResult> GenerateExcelReport(List<DocumentReportDTO> reportData)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Documents Report");
                worksheet.Columns().AdjustToContents();  
                worksheet.Column(3).Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";
                worksheet.Column(4).Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";

                worksheet.Cell(1, 1).Value = "Document ID";
                worksheet.Cell(1, 2).Value = "File Name";
                worksheet.Cell(1, 3).Value = "Created Date";
                worksheet.Cell(1, 4).Value = "Last Modified";
                worksheet.Cell(1, 5).Value = "File Size (bytes)";
                worksheet.Cell(1, 6).Value = "Is Public";
                worksheet.Cell(1, 7).Value = "Created By";
                worksheet.Cell(1, 9).Value = "Tags";

                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                for (int i = 0; i < reportData.Count; i++)
                {
                    var row = i + 2;
                    worksheet.Cell(row, 1).Value = reportData[i].DocumentId;
                    worksheet.Cell(row, 2).Value = reportData[i].FileName;
                    var createdDateCell = worksheet.Cell(row, 3);
                    createdDateCell.Value = reportData[i].CreatedDate;
                    createdDateCell.Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";

                    var modifiedDateCell = worksheet.Cell(row, 4);
                    if (reportData[i].LastModifiedDate.HasValue)
                    {
                        modifiedDateCell.Value = reportData[i].LastModifiedDate.Value;
                        modifiedDateCell.Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";
                    }
                    worksheet.Cell(row, 5).Value = reportData[i].FileSize;
                    worksheet.Cell(row, 6).Value = reportData[i].IsPublic;
                    worksheet.Cell(row, 7).Value = reportData[i].CreatedBy;
                    worksheet.Cell(row, 8).Value = reportData[i].Email;
                    worksheet.Cell(row, 9).Value = string.Join(", ", reportData[i].Tags);
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "documents_report.xlsx");
                }
            }
        }

        private async Task<FileContentResult> GeneratePdfReport(List<DocumentReportDTO> reportData)
        {
            using (var stream = new MemoryStream())
            {
                var writer = new PdfWriter(stream);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf, PageSize.A4.Rotate());
        document.SetMargins(20, 20, 20, 20);

                var title = new Paragraph("Documents Report")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(16);
        document.Add(title);
        document.Add(new Paragraph("\n"));

        float[] columnWidths = { 40, 150, 80, 80, 60, 40, 80, 120, 150 };
        var table = new Table(UnitValue.CreatePointArray(columnWidths));
        table.SetWidth(UnitValue.CreatePercentValue(100));

        var headerStyle = new Style()
            .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
            .SetFontSize(10);

        string[] headers = { "Doc ID", "File Name", "Created Date", "Last Modified",
                           "Size (bytes)", "Public", "Created By", "Email", "Tags" };
        foreach (var header in headers)
        {
            table.AddHeaderCell(
                new Cell()
                    .Add(new Paragraph(header))
                    .AddStyle(headerStyle)
                    .SetTextAlignment(TextAlignment.CENTER)
            );
        }

        foreach (var item in reportData)
        {
            table.AddCell(new Cell().Add(new Paragraph(item.DocumentId.ToString()))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(9));
            
            table.AddCell(new Cell().Add(new Paragraph(item.FileName))
                .SetFontSize(9));
            
            table.AddCell(new Cell().Add(new Paragraph(item.CreatedDate.ToString("yyyy-MM-dd HH:mm")))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(9));
            
            table.AddCell(new Cell().Add(new Paragraph(item.LastModifiedDate?.ToString("yyyy-MM-dd HH:mm") ?? ""))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(9));
            
            table.AddCell(new Cell().Add(new Paragraph(item.FileSize.ToString()))
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetFontSize(9));
            
            table.AddCell(new Cell().Add(new Paragraph(item.IsPublic.ToString()))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(9));
            
            table.AddCell(new Cell().Add(new Paragraph(item.CreatedBy))
                .SetFontSize(9));
            
            table.AddCell(new Cell().Add(new Paragraph(item.Email))
                .SetFontSize(9));

            table.AddCell(new Cell().Add(new Paragraph(string.Join(", ", item.Tags)))
        .SetFontSize(9));
                }

        document.Add(table);

                var footer = new Paragraph($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetFontSize(8);
        document.Add(footer);

        document.Close();

        return File(stream.ToArray(), "application/pdf", $"documents_report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
    }
        }
    }

}

