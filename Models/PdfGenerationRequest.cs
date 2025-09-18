namespace FastReportPdfServer.Models
{
    public class PdfGenerationRequest
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public Dictionary<string, object>? Parameters { get; set; }
        public string? TemplateName { get; set; }
    }

    public class QuotationData
    {
        public int QuotationId { get; set; }
        public int CustomerId { get; set; }
        public int CustomerProjectId { get; set; }
        public DateTime QuotationDate { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal TotalTax { get; set; }
        public decimal GrandTotal { get; set; }
        public string QuotationStatus { get; set; } = string.Empty;
        public string TermsAndCondition { get; set; } = string.Empty;
        public string PaymentTerms { get; set; } = string.Empty;
        public string ScopeOfWork { get; set; } = string.Empty;
        public List<QuotationItem> QuotationItems { get; set; } = new();
    }

    public class QuotationItem
    {
        public int? ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Dimension { get; set; } = string.Empty;
        public int UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public string UomCode { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal TaxPercent { get; set; }
        public decimal LineTotal { get; set; }
    }
}