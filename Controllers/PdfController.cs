using Microsoft.AspNetCore.Mvc;
using FastReport;
using FastReport.Export.PdfSimple;
using System.Data;
using System.Text.Json;

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

        [HttpGet("health")]
        public IActionResult Health()
        {
            var templatesDirectory = Path.Combine(_environment.ContentRootPath, "Templates");
            var directoryExists = Directory.Exists(templatesDirectory);
            
            var templates = new List<string>();
            if (directoryExists)
            {
                templates = Directory.GetFiles(templatesDirectory, "*.frx")
                    .Select(f => Path.GetFileNameWithoutExtension(f) ?? string.Empty)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToList();
            }
            
            return Ok(new { 
                status = "healthy", 
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                fastreportVersion = "OpenSource",
                templatesDirectory = templatesDirectory,
                directoryExists = directoryExists,
                availableTemplates = templates
            });
        }

        [HttpPost("generate")]
        public IActionResult GeneratePdf([FromBody] PdfGenerationRequest request)
        {
            try
            {
                // Validate request
                if (string.IsNullOrEmpty(request.TemplateName))
                {
                    return BadRequest(new { error = "Template name is required" });
                }

                if (request.Data.ValueKind == JsonValueKind.Undefined || request.Data.ValueKind == JsonValueKind.Null)
                {
                    return BadRequest(new { error = "Data is required" });
                }

                using var report = new Report();
                
                // Create DataSet from JSON data
                var dataSet = CreateDataSetFromJson(request.Data);
                
                // Register the dataset with the report
                report.RegisterData(dataSet, "Data");
                
                // Load the template
                var templatePath = Path.Combine(_environment.ContentRootPath, "Templates", $"{request.TemplateName}.frx");
                
                if (!System.IO.File.Exists(templatePath))
                {
                    return BadRequest(new { 
                        error = "Template file not found", 
                        templateName = request.TemplateName,
                        expectedPath = templatePath,
                        suggestion = $"Make sure {request.TemplateName}.frx exists in the Templates folder"
                    });
                }

                report.Load(templatePath);
                Console.WriteLine($"Template '{request.TemplateName}' loaded successfully");

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

                var fileName = $"{request.TemplateName}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                
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

        private DataSet CreateDataSetFromJson(JsonElement jsonData)
        {
            var dataSet = new DataSet("Data");

            foreach (var property in jsonData.EnumerateObject())
            {
                var tableName = property.Name;
                var tableData = property.Value;

                if (tableData.ValueKind == JsonValueKind.Array)
                {
                    // Handle array of objects (e.g., items, records)
                    var dataTable = CreateTableFromArray(tableName, tableData);
                    if (dataTable != null)
                    {
                        dataSet.Tables.Add(dataTable);
                    }
                }
                else if (tableData.ValueKind == JsonValueKind.Object)
                {
                    // Handle single object (e.g., header data)
                    var dataTable = CreateTableFromObject(tableName, tableData);
                    if (dataTable != null)
                    {
                        dataSet.Tables.Add(dataTable);
                    }
                }
            }

            return dataSet;
        }

        private DataTable? CreateTableFromArray(string tableName, JsonElement array)
        {
            if (array.GetArrayLength() == 0) return null;

            var dataTable = new DataTable(tableName);
            
            // Get all unique properties from all objects in the array
            var allProperties = new HashSet<string>();
            foreach (var item in array.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in item.EnumerateObject())
                    {
                        allProperties.Add(prop.Name);
                    }
                }
            }

            // Add columns
            foreach (var propName in allProperties)
            {
                dataTable.Columns.Add(propName, typeof(object));
            }

            // Add rows
            foreach (var item in array.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    var row = dataTable.NewRow();
                    foreach (var prop in item.EnumerateObject())
                    {
                        row[prop.Name] = ConvertJsonElementToValue(prop.Value);
                    }
                    dataTable.Rows.Add(row);
                }
            }

            return dataTable;
        }

        private DataTable CreateTableFromObject(string tableName, JsonElement obj)
        {
            var dataTable = new DataTable(tableName);

            // Add columns
            foreach (var prop in obj.EnumerateObject())
            {
                dataTable.Columns.Add(prop.Name, typeof(object));
            }

            // Add single row with the object data
            var row = dataTable.NewRow();
            foreach (var prop in obj.EnumerateObject())
            {
                row[prop.Name] = ConvertJsonElementToValue(prop.Value);
            }
            dataTable.Rows.Add(row);

            return dataTable;
        }

        private object ConvertJsonElementToValue(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Number:
                    if (element.TryGetInt64(out long longValue))
                        return longValue;
                    else if (element.TryGetDecimal(out decimal decimalValue))
                        return decimalValue;
                    else
                        return element.GetDouble();
                case JsonValueKind.String:
                    var stringValue = element.GetString();
                    // Try to parse as DateTime
                    if (DateTime.TryParse(stringValue, out DateTime dateValue))
                        return dateValue;
                    return stringValue ?? string.Empty;
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return DBNull.Value;
                default:
                    return element.ToString();
            }
        }
    }

    // Simple request model
    public class PdfGenerationRequest
    {
        public string TemplateName { get; set; } = string.Empty;
        public JsonElement Data { get; set; }
    }
}