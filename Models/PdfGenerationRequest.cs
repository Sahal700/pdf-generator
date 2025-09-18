namespace FastReportPdfServer.Models
{
    public class PdfGenerationRequest
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public Dictionary<string, object>? Parameters { get; set; }
        public string? TemplateName { get; set; }
    }
}