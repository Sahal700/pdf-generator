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

        [HttpPost("generate-quotation")]
        public IActionResult GenerateQuotationPdf([FromBody] QuotationData quotationData)
        {
            try
            {
                using var report = new Report();
                
                // Create dataset for quotation data
                var dataSet = new DataSet("Data");
                
                // Create QuotationData table
                var quotationTable = new DataTable("QuotationData");
                quotationTable.Columns.Add("quotation_id", typeof(int));
                quotationTable.Columns.Add("customer_id", typeof(int));
                quotationTable.Columns.Add("customer_project_id", typeof(int));
                quotationTable.Columns.Add("quotation_date", typeof(DateTime));
                quotationTable.Columns.Add("subtotal", typeof(decimal));
                quotationTable.Columns.Add("total_discount", typeof(decimal));
                quotationTable.Columns.Add("taxable_amount", typeof(decimal));
                quotationTable.Columns.Add("total_tax", typeof(decimal));
                quotationTable.Columns.Add("grand_total", typeof(decimal));
                quotationTable.Columns.Add("quotation_status", typeof(string));
                quotationTable.Columns.Add("terms_and_condition", typeof(string));
                quotationTable.Columns.Add("payment_terms", typeof(string));
                quotationTable.Columns.Add("scope_of_work", typeof(string));
                
                // Add quotation data row
                var quotationRow = quotationTable.NewRow();
                quotationRow["quotation_id"] = quotationData.QuotationId;
                quotationRow["customer_id"] = quotationData.CustomerId;
                quotationRow["customer_project_id"] = quotationData.CustomerProjectId;
                quotationRow["quotation_date"] = quotationData.QuotationDate;
                quotationRow["subtotal"] = quotationData.Subtotal;
                quotationRow["total_discount"] = quotationData.TotalDiscount;
                quotationRow["taxable_amount"] = quotationData.TaxableAmount;
                quotationRow["total_tax"] = quotationData.TotalTax;
                quotationRow["grand_total"] = quotationData.GrandTotal;
                quotationRow["quotation_status"] = quotationData.QuotationStatus;
                quotationRow["terms_and_condition"] = quotationData.TermsAndCondition;
                quotationRow["payment_terms"] = quotationData.PaymentTerms;
                quotationRow["scope_of_work"] = quotationData.ScopeOfWork;
                quotationTable.Rows.Add(quotationRow);
                
                // Create QuotationItems table
                var itemsTable = new DataTable("QuotationItems");
                itemsTable.Columns.Add("product_id", typeof(int));
                itemsTable.Columns.Add("product_name", typeof(string));
                itemsTable.Columns.Add("quantity", typeof(int));
                itemsTable.Columns.Add("dimension", typeof(string));
                itemsTable.Columns.Add("unit_id", typeof(int));
                itemsTable.Columns.Add("unit_name", typeof(string));
                itemsTable.Columns.Add("uom_code", typeof(string));
                itemsTable.Columns.Add("unit_price", typeof(decimal));
                itemsTable.Columns.Add("discount_percent", typeof(decimal));
                itemsTable.Columns.Add("tax_percent", typeof(decimal));
                itemsTable.Columns.Add("line_total", typeof(decimal));
                
                // Add quotation items
                foreach (var item in quotationData.QuotationItems)
                {
                    var itemRow = itemsTable.NewRow();
                    itemRow["product_id"] = item.ProductId ?? (object)DBNull.Value;
                    itemRow["product_name"] = item.ProductName;
                    itemRow["quantity"] = item.Quantity;
                    itemRow["dimension"] = item.Dimension;
                    itemRow["unit_id"] = item.UnitId;
                    itemRow["unit_name"] = item.UnitName;
                    itemRow["uom_code"] = item.UomCode;
                    itemRow["unit_price"] = item.UnitPrice;
                    itemRow["discount_percent"] = item.DiscountPercent;
                    itemRow["tax_percent"] = item.TaxPercent;
                    itemRow["line_total"] = item.LineTotal;
                    itemsTable.Rows.Add(itemRow);
                }
                
                dataSet.Tables.Add(quotationTable);
                dataSet.Tables.Add(itemsTable);
                
                // Register the dataset with the report
                report.RegisterData(dataSet, "Data");
                
                // Load the quotation template
                var templatePath = Path.Combine(_environment.ContentRootPath, "Templates", "quotation-template.frx");
                
                if (System.IO.File.Exists(templatePath))
                {
                    report.Load(templatePath);
                    Console.WriteLine("Quotation template loaded successfully");
                }
                else
                {
                    Console.WriteLine($"Quotation template not found at: {templatePath}");
                    // Use inline template as fallback
                    var inlineTemplate = CreateInlineQuotationTemplate();
                    report.LoadFromString(inlineTemplate);
                    Console.WriteLine("Using inline quotation template");
                }

                // Prepare the report
                report.Prepare();

                // Export to PDF
                using var pdfExport = new PDFSimpleExport();
                using var stream = new MemoryStream();
                
                pdfExport.Export(report, stream);
                var pdfBytes = stream.ToArray();

                if (pdfBytes.Length == 0)
                {
                    return StatusCode(500, new { error = "Generated quotation PDF is empty" });
                }

                var fileName = $"quotation_{quotationData.QuotationId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                
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
            var quotationTemplatePath = Path.Combine(_environment.ContentRootPath, "Templates", "quotation-template.frx");
            var templateExists = System.IO.File.Exists(templatePath);
            var quotationTemplateExists = System.IO.File.Exists(quotationTemplatePath);
            
            return Ok(new { 
                status = "healthy", 
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                fastreportVersion = "OpenSource",
                templatePath = templatePath,
                templateExists = templateExists,
                quotationTemplatePath = quotationTemplatePath,
                quotationTemplateExists = quotationTemplateExists
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

        private string CreateInlineQuotationTemplate()
        {
            return @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""urn:fastreport"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""urn:fastreport https://www.fast-report.com/xml/fastreport.xsd"" ScriptLanguage=""CSharp"" ReportInfo.Created=""01/01/2024 00:00:00"" ReportInfo.Modified=""01/01/2024 00:00:00"" ReportInfo.CreatorVersion=""1.0.0.0"">
  <Dictionary>
    <TableDataSource Name=""QuotationData"" ReferenceName=""Data.QuotationData"" DataType=""System.Int32"" Enabled=""true"">
      <Column Name=""quotation_id"" DataType=""System.Int32""/>
      <Column Name=""customer_id"" DataType=""System.Int32""/>
      <Column Name=""customer_project_id"" DataType=""System.Int32""/>
      <Column Name=""quotation_date"" DataType=""System.DateTime""/>
      <Column Name=""subtotal"" DataType=""System.Decimal""/>
      <Column Name=""total_discount"" DataType=""System.Decimal""/>
      <Column Name=""taxable_amount"" DataType=""System.Decimal""/>
      <Column Name=""total_tax"" DataType=""System.Decimal""/>
      <Column Name=""grand_total"" DataType=""System.Decimal""/>
      <Column Name=""quotation_status"" DataType=""System.String""/>
      <Column Name=""terms_and_condition"" DataType=""System.String""/>
      <Column Name=""payment_terms"" DataType=""System.String""/>
      <Column Name=""scope_of_work"" DataType=""System.String""/>
    </TableDataSource>
    <TableDataSource Name=""QuotationItems"" ReferenceName=""Data.QuotationItems"" DataType=""System.Int32"" Enabled=""true"">
      <Column Name=""product_id"" DataType=""System.Int32""/>
      <Column Name=""product_name"" DataType=""System.String""/>
      <Column Name=""quantity"" DataType=""System.Int32""/>
      <Column Name=""dimension"" DataType=""System.String""/>
      <Column Name=""unit_id"" DataType=""System.Int32""/>
      <Column Name=""unit_name"" DataType=""System.String""/>
      <Column Name=""uom_code"" DataType=""System.String""/>
      <Column Name=""unit_price"" DataType=""System.Decimal""/>
      <Column Name=""discount_percent"" DataType=""System.Decimal""/>
      <Column Name=""tax_percent"" DataType=""System.Decimal""/>
      <Column Name=""line_total"" DataType=""System.Decimal""/>
    </TableDataSource>
  </Dictionary>
  
  <ReportPage Name=""Page1"" Landscape=""false"" PaperWidth=""210"" PaperHeight=""297"" RawPaperSize=""9"" LeftMargin=""10"" TopMargin=""10"" RightMargin=""10"" BottomMargin=""10"">
    
    <!-- Header Section -->
    <ReportTitleBand Name=""ReportTitle1"" Width=""718.2"" Height=""113.4"">
      <TextObject Name=""CompanyName"" Left=""0"" Top=""9.45"" Width=""360"" Height=""28.35"" Text=""Your Company Name"" Font=""Arial, 16pt, style=Bold"" TextFill.Color=""DarkBlue""/>
      <TextObject Name=""CompanyAddress"" Left=""0"" Top=""37.8"" Width=""360"" Height=""37.8"" Text=""Company Address Line 1&#13;&#10;Company Address Line 2&#13;&#10;Phone: +XX-XXXXXXXXX | Email: info@company.com"" Font=""Arial, 9pt"" TextFill.Color=""Black""/>
      
      <TextObject Name=""QuotationTitle"" Left=""453.6"" Top=""9.45"" Width=""264.6"" Height=""28.35"" Text=""QUOTATION"" HorzAlign=""Right"" Font=""Arial, 18pt, style=Bold"" TextFill.Color=""DarkBlue""/>
      <TextObject Name=""QuotationNumber"" Left=""453.6"" Top=""37.8"" Width=""264.6"" Height=""18.9"" Text=""Quote #: [QuotationData.quotation_id]"" HorzAlign=""Right"" Font=""Arial, 10pt, style=Bold""/>
      <TextObject Name=""QuotationDate"" Left=""453.6"" Top=""56.7"" Width=""264.6"" Height=""18.9"" Text=""Date: [QuotationData.quotation_date]"" HorzAlign=""Right"" Font=""Arial, 10pt""/>
      <TextObject Name=""QuotationStatus"" Left=""453.6"" Top=""75.6"" Width=""264.6"" Height=""18.9"" Text=""Status: [QuotationData.quotation_status]"" HorzAlign=""Right"" Font=""Arial, 10pt""/>
      
      <LineObject Name=""HeaderLine"" Left=""0"" Top=""103.95"" Width=""718.2"" Height=""0"" Border.Lines=""Top"" Border.Color=""DarkBlue"" Border.Width=""2""/>
    </ReportTitleBand>
    
    <!-- Customer Information -->
    <DataBand Name=""CustomerInfo"" Top=""117.4"" Width=""718.2"" Height=""75.6"" DataSource=""QuotationData"" PrintIfDetailEmpty=""false"">
      <TextObject Name=""CustomerLabel"" Left=""0"" Top=""9.45"" Width=""113.4"" Height=""18.9"" Text=""Bill To:"" Font=""Arial, 10pt, style=Bold""/>
      <TextObject Name=""CustomerDetails"" Left=""0"" Top=""28.35"" Width=""340"" Height=""37.8"" Text=""Customer ID: [QuotationData.customer_id]&#13;&#10;Project ID: [QuotationData.customer_project_id]"" Font=""Arial, 9pt""/>
      
      <TextObject Name=""ScopeLabel"" Left=""378"" Top=""9.45"" Width=""113.4"" Height=""18.9"" Text=""Scope of Work:"" Font=""Arial, 10pt, style=Bold""/>
      <TextObject Name=""ScopeDetails"" Left=""378"" Top=""28.35"" Width=""340.2"" Height=""37.8"" Text=""[QuotationData.scope_of_work]"" Font=""Arial, 9pt"" WordWrap=""true""/>
      
      <LineObject Name=""CustomerLine"" Left=""0"" Top=""66.15"" Width=""718.2"" Height=""0"" Border.Lines=""Top"" Border.Color=""LightGray""/>
    </DataBand>
    
    <!-- Items Table Header -->
    <GroupHeaderBand Name=""ItemsHeader"" Top=""197"" Width=""718.2"" Height=""37.8"">
      <TextObject Name=""ItemHeaderBg"" Left=""0"" Top=""9.45"" Width=""718.2"" Height=""28.35"" Fill.Color=""WhiteSmoke"" Border.Lines=""All"" Border.Color=""Gray""/>
      <TextObject Name=""ItemNoHeader"" Left=""9.45"" Top=""14.17"" Width=""37.8"" Height=""18.9"" Text=""S.No"" Font=""Arial, 9pt, style=Bold"" HorzAlign=""Center""/>
      <TextObject Name=""ProductHeader"" Left=""56.7"" Top=""14.17"" Width=""189"" Height=""18.9"" Text=""Product/Service"" Font=""Arial, 9pt, style=Bold""/>
      <TextObject Name=""DimensionHeader"" Left=""255.15"" Top=""14.17"" Width=""75.6"" Height=""18.9"" Text=""Dimension"" Font=""Arial, 9pt, style=Bold"" HorzAlign=""Center""/>
      <TextObject Name=""QtyHeader"" Left=""340.2"" Top=""14.17"" Width=""47.25"" Height=""18.9"" Text=""Qty"" Font=""Arial, 9pt, style=Bold"" HorzAlign=""Center""/>
      <TextObject Name=""UnitHeader"" Left=""396.9"" Top=""14.17"" Width=""47.25"" Height=""18.9"" Text=""Unit"" Font=""Arial, 9pt, style=Bold"" HorzAlign=""Center""/>
      <TextObject Name=""RateHeader"" Left=""453.6"" Top=""14.17"" Width=""66.15"" Height=""18.9"" Text=""Rate"" Font=""Arial, 9pt, style=Bold"" HorzAlign=""Right""/>
      <TextObject Name=""DiscountHeader"" Left=""529.2"" Top=""14.17"" Width=""56.7"" Height=""18.9"" Text=""Disc%"" Font=""Arial, 9pt, style=Bold"" HorzAlign=""Center""/>
      <TextObject Name=""TaxHeader"" Left=""594.45"" Top=""14.17"" Width=""47.25"" Height=""18.9"" Text=""Tax%"" Font=""Arial, 9pt, style=Bold"" HorzAlign=""Center""/>
      <TextObject Name=""AmountHeader"" Left=""651.15"" Top=""14.17"" Width=""66.15"" Height=""18.9"" Text=""Amount"" Font=""Arial, 9pt, style=Bold"" HorzAlign=""Right""/>
    </GroupHeaderBand>
    
    <!-- Items Data -->
    <DataBand Name=""ItemsData"" Top=""238.8"" Width=""718.2"" Height=""28.35"" DataSource=""QuotationItems"">
      <TextObject Name=""ItemRowBg"" Left=""0"" Top=""0"" Width=""718.2"" Height=""28.35"" Border.Lines=""All"" Border.Color=""LightGray""/>
      <TextObject Name=""ItemNo"" Left=""9.45"" Top=""4.72"" Width=""37.8"" Height=""18.9"" Text=""[Row#]"" Font=""Arial, 9pt"" HorzAlign=""Center""/>
      <TextObject Name=""ProductName"" Left=""56.7"" Top=""4.72"" Width=""189"" Height=""18.9"" Text=""[QuotationItems.product_name]"" Font=""Arial, 9pt""/>
      <TextObject Name=""Dimension"" Left=""255.15"" Top=""4.72"" Width=""75.6"" Height=""18.9"" Text=""[QuotationItems.dimension]"" Font=""Arial, 9pt"" HorzAlign=""Center""/>
      <TextObject Name=""Quantity"" Left=""340.2"" Top=""4.72"" Width=""47.25"" Height=""18.9"" Text=""[QuotationItems.quantity]"" Font=""Arial, 9pt"" HorzAlign=""Center""/>
      <TextObject Name=""Unit"" Left=""396.9"" Top=""4.72"" Width=""47.25"" Height=""18.9"" Text=""[QuotationItems.uom_code]"" Font=""Arial, 9pt"" HorzAlign=""Center""/>
      <TextObject Name=""UnitPrice"" Left=""453.6"" Top=""4.72"" Width=""66.15"" Height=""18.9"" Text=""[QuotationItems.unit_price]"" Format=""Currency"" Font=""Arial, 9pt"" HorzAlign=""Right""/>
      <TextObject Name=""DiscountPercent"" Left=""529.2"" Top=""4.72"" Width=""56.7"" Height=""18.9"" Text=""[QuotationItems.discount_percent]%"" Font=""Arial, 9pt"" HorzAlign=""Center""/>
      <TextObject Name=""TaxPercent"" Left=""594.45"" Top=""4.72"" Width=""47.25"" Height=""18.9"" Text=""[QuotationItems.tax_percent]%"" Font=""Arial, 9pt"" HorzAlign=""Center""/>
      <TextObject Name=""LineTotal"" Left=""651.15"" Top=""4.72"" Width=""66.15"" Height=""18.9"" Text=""[QuotationItems.line_total]"" Format=""Currency"" Font=""Arial, 9pt"" HorzAlign=""Right""/>
    </DataBand>
    
    <!-- Summary Section -->
    <GroupFooterBand Name=""Summary"" Width=""718.2"" Height=""132.3"">
      <LineObject Name=""SummaryLine"" Left=""0"" Top=""9.45"" Width=""718.2"" Height=""0"" Border.Lines=""Top"" Border.Color=""Gray"" Border.Width=""1""/>
      
      <TextObject Name=""SubtotalLabel"" Left=""529.2"" Top=""18.9"" Width=""94.5"" Height=""18.9"" Text=""Subtotal:"" Font=""Arial, 10pt, style=Bold"" HorzAlign=""Right""/>
      <TextObject Name=""SubtotalValue"" Left=""632.25"" Top=""18.9"" Width=""85.05"" Height=""18.9"" Text=""[QuotationData.subtotal]"" Format=""Currency"" Font=""Arial, 10pt"" HorzAlign=""Right""/>
      
      <TextObject Name=""DiscountLabel"" Left=""529.2"" Top=""37.8"" Width=""94.5"" Height=""18.9"" Text=""Discount:"" Font=""Arial, 10pt"" HorzAlign=""Right""/>
      <TextObject Name=""DiscountValue"" Left=""632.25"" Top=""37.8"" Width=""85.05"" Height=""18.9"" Text=""[QuotationData.total_discount]"" Format=""Currency"" Font=""Arial, 10pt"" HorzAlign=""Right""/>
      
      <TextObject Name=""TaxableLabel"" Left=""529.2"" Top=""56.7"" Width=""94.5"" Height=""18.9"" Text=""Taxable Amount:"" Font=""Arial, 10pt"" HorzAlign=""Right""/>
      <TextObject Name=""TaxableValue"" Left=""632.25"" Top=""56.7"" Width=""85.05"" Height=""18.9"" Text=""[QuotationData.taxable_amount]"" Format=""Currency"" Font=""Arial, 10pt"" HorzAlign=""Right""/>
      
      <TextObject Name=""TaxLabel"" Left=""529.2"" Top=""75.6"" Width=""94.5"" Height=""18.9"" Text=""Tax:"" Font=""Arial, 10pt"" HorzAlign=""Right""/>
      <TextObject Name=""TaxValue"" Left=""632.25"" Top=""75.6"" Width=""85.05"" Height=""18.9"" Text=""[QuotationData.total_tax]"" Format=""Currency"" Font=""Arial, 10pt"" HorzAlign=""Right""/>
      
      <LineObject Name=""GrandTotalLine"" Left=""529.2"" Top=""94.5"" Width=""188.55"" Height=""0"" Border.Lines=""Top"" Border.Color=""Black"" Border.Width=""1""/>
      
      <TextObject Name=""GrandTotalLabel"" Left=""529.2"" Top=""103.95"" Width=""94.5"" Height=""18.9"" Text=""Grand Total:"" Font=""Arial, 12pt, style=Bold"" HorzAlign=""Right""/>
      <TextObject Name=""GrandTotalValue"" Left=""632.25"" Top=""103.95"" Width=""85.05"" Height=""18.9"" Text=""[QuotationData.grand_total]"" Format=""Currency"" Font=""Arial, 12pt, style=Bold"" HorzAlign=""Right""/>
    </GroupFooterBand>
    
    <!-- Terms and Conditions -->
    <GroupFooterBand Name=""Terms"" Width=""718.2"" Height=""94.5"">
      <LineObject Name=""TermsLine"" Left=""0"" Top=""9.45"" Width=""718.2"" Height=""0"" Border.Lines=""Top"" Border.Color=""LightGray""/>
      
      <TextObject Name=""PaymentTermsLabel"" Left=""0"" Top=""18.9"" Width=""113.4"" Height=""18.9"" Text=""Payment Terms:"" Font=""Arial, 10pt, style=Bold""/>
      <TextObject Name=""PaymentTermsValue"" Left=""122.85"" Top=""18.9"" Width=""264.6"" Height=""18.9"" Text=""[QuotationData.payment_terms]"" Font=""Arial, 10pt""/>
      
      <TextObject Name=""TermsLabel"" Left=""0"" Top=""47.25"" Width=""113.4"" Height=""18.9"" Text=""Terms &amp; Conditions:"" Font=""Arial, 10pt, style=Bold""/>
      <TextObject Name=""TermsValue"" Left=""0"" Top=""66.15"" Width=""718.2"" Height=""28.35"" Text=""[QuotationData.terms_and_condition]"" Font=""Arial, 9pt"" WordWrap=""true""/>
    </GroupFooterBand>
    
    <!-- Footer -->
    <PageFooterBand Name=""PageFooter1"" Top=""567"" Width=""718.2"" Height=""47.25"">
      <LineObject Name=""FooterLine"" Left=""0"" Top=""9.45"" Width=""718.2"" Height=""0"" Border.Lines=""Top"" Border.Color=""LightGray""/>
      <TextObject Name=""PageNumber"" Left=""529.2"" Top=""18.9"" Width=""189"" Height=""18.9"" Text=""Page [Page] of [TotalPages#]"" HorzAlign=""Right"" Font=""Arial, 9pt"" TextFill.Color=""Gray""/>
      <TextObject Name=""FooterText"" Left=""0"" Top=""18.9"" Width=""302.4"" Height=""18.9"" Text=""This quotation is valid for 30 days from the date of issue"" Font=""Arial, 9pt"" TextFill.Color=""Gray""/>
      <TextObject Name=""Signature"" Left=""453.6"" Top=""28.35"" Width=""264.6"" Height=""18.9"" Text=""Authorized Signature: ________________"" Font=""Arial, 9pt"" HorzAlign=""Right""/>
    </PageFooterBand>
    
  </ReportPage>
</Report>";
        }
    }
}