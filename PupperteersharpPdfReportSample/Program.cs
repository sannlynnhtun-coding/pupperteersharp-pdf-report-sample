using HandlebarsDotNet;
using PupperteersharpPdfReportSample.Services;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<ReportService>();

// The BrowserSetupService has been removed as the logic is now in ReportService.

Handlebars.RegisterHelper("formatDate", (writer, context, parameters) =>
{
    if (parameters.Length > 0 && parameters[0] is DateOnly date)
    {
        writer.WriteSafeString(date.ToString("MMMM dd, yyyy"));
    }
    else if (parameters.Length > 0 && parameters[0] is DateTime dateTime)
    {
        writer.WriteSafeString(dateTime.ToString("MMMM dd, yyyy"));
    }
});

Handlebars.RegisterHelper("formatCurrency", (writer, context, parameters) =>
{
    if (parameters.Length > 0 && parameters[0] is decimal money)
    {
        writer.WriteSafeString(money.ToString("C", new CultureInfo("en-US")));
    }
});

Handlebars.RegisterHelper("multiply", (writer, context, parameters) =>
{
    if (parameters.Length > 1 && parameters[0] is decimal price && parameters[1] is int quantity)
    {
        writer.WriteSafeString((price * quantity));
    }
});

Handlebars.RegisterHelper("now", (writer, context, parameters) =>
{
    writer.WriteSafeString(DateTime.Now.ToString("yyyy-MM-dd"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/reports/users", async (ReportService reportService) =>
{
    var users = new[]
    {
        new { Name = "John Doe", Email = "john.doe@example.com", Age = 32 },
        new { Name = "Jane Smith", Email = "jane.smith@example.com", Age = 28 },
        new { Name = "Sam Wilson", Email = "sam.wilson@example.com", Age = 45 }
    };
    var htmlContent = await reportService.GenerateReportHtmlAsync("report-template.html", users);
    var pdfBytes = await reportService.GeneratePdfFromHtmlAsync(htmlContent);
    return Results.File(pdfBytes, "application/pdf", "user-report.pdf");
});

app.MapGet("/reports/invoice", async (ReportService reportService) =>
{
    var invoiceData = new
    {
        Number = "INV-2023-001",
        IssuedDate = new DateOnly(2023, 10, 27),
        DueDate = new DateOnly(2023, 11, 26),
        SellerAddress = new { CompanyName = "My Awesome Company", Street = "123 Main St", City = "Metropolis", State = "CA 12345", Email = "sales@awesome.com" },
        CustomerAddress = new { CompanyName = "Valued Customer Inc.", Street = "456 Client Ave", City = "Gotham", State = "NY 54321", Email = "purchasing@valuedcustomer.com" },
        LineItems = new[]
        {
            new { Name = "Premium Widget", Quantity = 2, Price = 150.00m },
            new { Name = "Standard Gizmo", Quantity = 5, Price = 75.50m },
            new { Name = "Support Contract", Quantity = 1, Price = 500.00m }
        },
        Subtotal = 1177.50m,
        Total = 1177.50m,
        LogoBase64 = ""
    };
    var htmlContent = await reportService.GenerateReportHtmlAsync("invoice-template.html", invoiceData);
    var pdfBytes = await reportService.GeneratePdfFromHtmlAsync(htmlContent, withHeaderAndFooter: true);
    return Results.File(pdfBytes, "application/pdf", $"invoice-{invoiceData.Number}.pdf");
});

app.Run();