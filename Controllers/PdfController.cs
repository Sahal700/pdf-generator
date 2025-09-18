using Microsoft.AspNetCore.Mvc;
using FastReport;
using FastReport.Export.PdfSimple;
using FastReportPdfServer.Models;
using System.Data;

namespace FastReportPdfServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PdfController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        public PdfController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpPost("generate")]
        public IActionResult GeneratePdf([FromBody] PdfGenerationRequest request)
        {
            try
            {
                using var report = new Report();
                Console.WriteLine(report);
                
                // Create a simple dataset for the report
                var dataSet = new DataSet("Data");
                var dataTable = new DataTable("ReportData");
                
                // Add columns matching the template
                dataTable.Columns.Add("Title", typeof(string));
                dataTable.Columns.Add("Content", typeof(string));
                dataTable.Columns.Add("GeneratedDate", typeof(DateTime));
                
                // Add data row
                var row = dataTable.NewRow();
                row["Title"] = request.Title ?? "Sample Report";
                row["Content"] = request.Content ?? "This is sample content for the PDF report.";
                row["GeneratedDate"] = DateTime.Now;
                dataTable.Rows.Add(row);

                Console.WriteLine(row);
                
                dataSet.Tables.Add(dataTable);
                
                // Register the dataset with the report
                report.RegisterData(dataSet, "Data");
                
                // Load the template
                var templatePath = Path.Combine(_environment.ContentRootPath, "Templates", "simple-template.frx");
                
                if (System.IO.File.Exists(templatePath))
                {
                    report.Load(templatePath);
                    Console.WriteLine("Template loaded successfully");
                }
                else
                {
                    Console.WriteLine($"Template not found at: {templatePath}");
                    return BadRequest(new { 
                        error = "Template file not found", 
                        expectedPath = templatePath,
                        suggestion = "Make sure simple-template.frx exists in the Templates folder"
                    });
                }

                // Prepare the report (this processes the template with data)
                report.Prepare();

                // Export to PDF
                using var pdfExport = new PDFSimpleExport();
                using var stream = new MemoryStream();
                
                pdfExport.Export(report, stream);
                var pdfBytes = stream.ToArray();

                if (pdfBytes.Length == 0)
                {
                    return StatusCode(500, new { error = "Generated PDF is empty" });
                }

                var fileName = $"report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    error = ex.Message,
                    stackTrace = ex.StackTrace,
                    innerError = ex.InnerException?.Message,
                    type = ex.GetType().Name
                });
            }
        }

        [HttpPost("generate-fallback")]
        public IActionResult GenerateFallbackPdf([FromBody] PdfGenerationRequest request)
        {
            try
            {
                // This endpoint works without external template files
                using var report = new Report();
                
                // Create inline template
                var templateXml = CreateInlineTemplate(request);
                report.LoadFromString(templateXml);

                // Prepare and export
                report.Prepare();
                
                using var pdfExport = new PDFSimpleExport();
                using var stream = new MemoryStream();
                pdfExport.Export(report, stream);
                
                var fileName = $"fallback_report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    error = ex.Message,
                    details = ex.InnerException?.Message 
                });
            }
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            var templatePath = Path.Combine(_environment.ContentRootPath, "Templates", "simple-template.frx");
            var templateExists = System.IO.File.Exists(templatePath);
            
            return Ok(new { 
                status = "healthy", 
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                fastreportVersion = "OpenSource",
                templatePath = templatePath,
                templateExists = templateExists
            });
        }

        [HttpGet("template-check")]
        public IActionResult CheckTemplate()
        {
            var templatePath = Path.Combine(_environment.ContentRootPath, "Templates", "simple-template.frx");
            var templateExists = System.IO.File.Exists(templatePath);
            
            var templatesDirectory = Path.Combine(_environment.ContentRootPath, "Templates");
            var directoryExists = Directory.Exists(templatesDirectory);
            
            var files = directoryExists ? Directory.GetFiles(templatesDirectory) : new string[0];
            
            return Ok(new {
                templatePath = templatePath,
                templateExists = templateExists,
                templatesDirectory = templatesDirectory,
                directoryExists = directoryExists,
                filesInDirectory = files,
                contentRootPath = _environment.ContentRootPath
            });
        }

        private string CreateInlineTemplate(PdfGenerationRequest request)
        {
            return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""urn:fastreport"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""urn:fastreport https://www.fast-report.com/xml/fastreport.xsd"" ScriptLanguage=""CSharp"">
  <Dictionary/>
  <ReportPage Name=""Page1"" Landscape=""false"" PaperWidth=""210"" PaperHeight=""297"" RawPaperSize=""9"" LeftMargin=""10"" TopMargin=""10"" RightMargin=""10"" BottomMargin=""10"">
    <ReportTitleBand Name=""ReportTitle1"" Width=""718.2"" Height=""75.6"">
      <TextObject Name=""Text1"" Left=""0"" Top=""0"" Width=""718.2"" Height=""37.8"" Text=""{request.Title ?? "Sample Report"}"" HorzAlign=""Center"" Font=""Arial, 16pt, style=Bold""/>
      <TextObject Name=""Text2"" Left=""0"" Top=""37.8"" Width=""718.2"" Height=""18.9"" Text=""Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"" HorzAlign=""Center"" Font=""Arial, 10pt""/>
    </ReportTitleBand>
    <DataBand Name=""Data1"" Top=""79.6"" Width=""718.2"" Height=""151.2"">
      <TextObject Name=""Text3"" Left=""0"" Top=""0"" Width=""718.2"" Height=""151.2"" Text=""{request.Content ?? "Default Content"}"" Font=""Arial, 12pt"" WordWrap=""true""/>
    </DataBand>
  </ReportPage>
</Report>";
        }
    }
}