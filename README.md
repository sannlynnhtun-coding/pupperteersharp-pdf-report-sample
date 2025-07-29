# PDF Reporting with PuppeteerSharp and Handlebars.NET

This guide will walk you through creating a complete ASP.NET Core Minimal API project that generates dynamic PDF reports from HTML templates.

### **Step 1: Create the Project**

1. Open your terminal or command prompt.
2. Create a new ASP.NET Core Minimal API project by running the following command.
    
    ```
    dotnet new web -n PupperteersharpPdfReportSample
    
    ```
    
3. Navigate into the newly created project directory.
    
    ```
    cd PupperteersharpPdfReportSample
    
    ```
    
4. Create a `Services` folder inside your project directory.
    
    ```
    mkdir Services
    
    ```
    

Your initial project structure should look like this:

```
PupperteersharpPdfReportSample/
├── Services/
├── appsettings.Development.json
├── appsettings.json
├── obj/
├── bin/
├── Properties/
├── Program.cs
└── PupperteersharpPdfReportSample.csproj

```

### **Step 2: Add Required NuGet Packages**

Next, you need to add the `PuppeteerSharp` and `Handlebars.Net` libraries to your project. These are used for converting HTML to PDF and for templating, respectively.

Run these commands in your terminal:

```
dotnet add package PuppeteerSharp
dotnet add package Handlebars.Net

```

### **Step 3: Add the Code Files**

Now, let's add the code for each file.

### 1. Project File (`PupperteersharpPdfReportSample.csproj`)

Open the `.csproj` file and ensure it looks like the following. The commands in the previous step should have already configured this for you. This file defines the project's dependencies.

```
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Handlebars.Net" Version="2.1.4" />
    <PackageReference Include="PuppeteerSharp" Version="18.0.2" />
  </ItemGroup>

  <ItemGroup>
    <None Update="report-template.html">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Update="invoice-template.html">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>

```

### 2. Report Service (`Services/ReportService.cs`)

This service handles the core logic of populating the HTML templates with data and converting the final HTML to a PDF. It now also ensures the browser binaries are downloaded before use.

Create a new file named `ReportService.cs` inside the `Services` folder and add the following code:

```
using HandlebarsDotNet;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace PupperteersharpPdfReportSample.Services
{
    public class ReportService
    {
        public async Task<string> GenerateReportHtmlAsync(string templateName, object data)
        {
            var templateSource = await File.ReadAllTextAsync(templateName);
            var compiledTemplate = Handlebars.Compile(templateSource);
            return compiledTemplate(data);
        }

        public async Task<byte[]> GeneratePdfFromHtmlAsync(string htmlContent, bool withHeaderAndFooter = false)
        {
            // Download the browser if it's not already present.
            var browserFetcher = new BrowserFetcher();
            Console.WriteLine("Ensuring browser is downloaded...");
            await browserFetcher.DownloadAsync();
            Console.WriteLine("Browser is ready.");

            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                // The following args are recommended for running in server/container environments.
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
            });
            await using var page = await browser.NewPageAsync();

            await page.SetContentAsync(htmlContent);
            await page.EvaluateExpressionHandleAsync("document.fonts.ready");

            var pdfOptions = new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true,
            };

            if (withHeaderAndFooter)
            {
                pdfOptions.DisplayHeaderFooter = true;
                pdfOptions.HeaderTemplate = """
                    <div style="font-size: 10px; font-family: Arial; text-align: center; width: 100%; padding: 0 20px;">
                        Invoice Report - <span class="title"></span>
                    </div>
                """;
                pdfOptions.FooterTemplate = """
                    <div style="font-size: 10px; font-family: Arial; text-align: center; width: 100%; padding: 0 20px;">
                        <span>Page <span class="pageNumber"></span> of <span class="totalPages"></span></span>
                    </div>
                """;
                pdfOptions.MarginOptions = new MarginOptions
                {
                    Top = "50px",
                    Bottom = "50px",
                    Left = "20px",
                    Right = "20px"
                };
            }

            var pdfBytes = await page.PdfDataAsync(pdfOptions);
            return pdfBytes;
        }
    }
}

```

### 3. Simple Report Template (`report-template.html`)

This is a basic HTML template for a user list. Create this file in the root of your project directory.

```
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>User Report</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; color: #333; }
        h1 { text-align: center; color: #4a4a4a; }
        table { width: 100%; border-collapse: collapse; margin-top: 20px; }
        th, td { border: 1px solid #ddd; padding: 12px; text-align: left; }
        th { background-color: #f2f2f2; font-weight: bold; }
        tr:nth-child(even) { background-color: #f9f9f9; }
        tr:hover { background-color: #f1f1f1; }
        .footer { text-align: center; margin-top: 30px; font-size: 0.8em; color: #777; }
    </style>
</head>
<body>
    <h1>User Report</h1>
    <table>
        <thead>
            <tr>
                <th>Name</th>
                <th>Email</th>
                <th>Age</th>
            </tr>
        </thead>
        <tbody>
            {{#each this}}
            <tr>
                <td>{{Name}}</td>
                <td>{{Email}}</td>
                <td>{{Age}}</td>
            </tr>
            {{/each}}
        </tbody>
    </table>
    <div class="footer">
        Report Generated on: {{formatDate (now)}}
    </div>
</body>
</html>

```

### 4. Invoice Template (`invoice-template.html`)

This is a more complex template for generating a styled invoice. Create this file in the root of your project directory.

```
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8" />
    <title>Invoice #{{Number}}</title>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, 'Open Sans', 'Helvetica Neue', sans-serif; margin: 0; padding: 0; color: #333; font-size: 14px; line-height: 1.6; }
        .invoice-container { max-width: 800px; margin: auto; padding: 30px; border: 1px solid #eee; box-shadow: 0 0 10px rgba(0, 0, 0, .15); }
        .invoice-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 30px; }
        .invoice-title { font-size: 45px; line-height: 1.2; color: #333; margin: 0; }
        .header-details p { margin: 0; }
        .addresses { display: flex; justify-content: space-between; margin-bottom: 40px; }
        .address-box { width: 48%; }
        .address-title { font-weight: bold; border-bottom: 1px solid #eee; padding-bottom: 5px; margin-bottom: 10px; }
        .items-table { width: 100%; border-collapse: collapse; margin-bottom: 40px; }
        .items-table th, .items-table td { border-bottom: 1px solid #eee; padding: 12px; text-align: left; }
        .items-table th { background-color: #f8f8f8; font-weight: bold; border-top: 1px solid #eee; }
        .items-table .align-right { text-align: right; }
        .totals { display: flex; justify-content: flex-end; }
        .totals-container { width: 50%; min-width: 250px; }
        .totals-row { display: flex; justify-content: space-between; padding: 8px 0; }
        .totals-row.total { font-weight: bold; font-size: 1.2em; border-top: 2px solid #333; padding-top: 10px; }
    </style>
</head>
<body>
    <div class="invoice-container">
        <div class="invoice-header">
            <div class="header-details">
                <h1 class="invoice-title">INVOICE</h1>
                <p><strong>Invoice #:</strong> {{Number}}</p>
                <p><strong>Issued:</strong> {{formatDate IssuedDate}}</p>
                <p><strong>Due:</strong> {{formatDate DueDate}}</p>
            </div>
            <div>
                {{#if LogoBase64}}<img src="data:image/png;base64,{{LogoBase64}}" alt="Company Logo" style="max-width: 150px;" />{{else}}<div style="width: 150px; height: 75px; background-color: #f2f2f2; display: flex; align-items: center; justify-content: center; color: #ccc;">Logo</div>{{/if}}
            </div>
        </div>
        <div class="addresses">
            <div class="address-box">
                <p class="address-title">From:</p>
                <p><strong>{{SellerAddress.CompanyName}}</strong></p>
                <p>{{SellerAddress.Street}}</p>
                <p>{{SellerAddress.City}}, {{SellerAddress.State}}</p>
                <p>{{SellerAddress.Email}}</p>
            </div>
            <div class="address-box">
                <p class="address-title">To:</p>
                <p><strong>{{CustomerAddress.CompanyName}}</strong></p>
                <p>{{CustomerAddress.Street}}</p>
                <p>{{CustomerAddress.City}}, {{CustomerAddress.State}}</p>
                <p>{{CustomerAddress.Email}}</p>
            </div>
        </div>
        <table class="items-table">
            <thead><tr><th>Description</th><th>Qty</th><th class="align-right">Price</th><th class="align-right">Total</th></tr></thead>
            <tbody>
                {{#each LineItems}}
                <tr><td>{{Name}}</td><td>{{Quantity}}</td><td class="align-right">{{formatCurrency Price}}</td><td class="align-right">{{formatCurrency (multiply Price Quantity)}}</td></tr>
                {{/each}}
            </tbody>
        </table>
        <div class="totals">
            <div class="totals-container">
                <div class="totals-row"><span>Subtotal:</span><span class="align-right">{{formatCurrency Subtotal}}</span></div>
                <div class="totals-row"><span>Tax (0%):</span><span class="align-right">{{formatCurrency 0}}</span></div>
                <div class="totals-row total"><span>Total:</span><span class="align-right">{{formatCurrency Total}}</span></div>
            </div>
        </div>
    </div>
</body>
</html>

```

### 5. Main Application Logic (`Program.cs`)

Finally, replace the contents of `Program.cs` with the code below. This file sets up the API endpoints, registers services, and defines the Handlebars helpers. Note that we have removed the `BrowserSetupService`.

```
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

```

### **Step 4: Run the Application**

You are now ready to run the project. Execute the following command in your terminal from the project's root directory:

```
dotnet run

```

The first time you run an endpoint, you will see messages in the console indicating that the browser binaries are being downloaded. This is expected and will only happen on the first run (or if the binaries are deleted).

### **Step 5: Test the PDF Generation**

Once the application is running, you can test the endpoints.

- **Simple User Report:**
Open your web browser and navigate to: `https://localhost:<port>/reports/users`
(Replace `<port>` with the port number shown in your terminal, e.g., 7287).
- **Complex Invoice Report:**
In your browser, navigate to: `https://localhost:<port>/reports/invoice`

In both cases, your browser should download the generated PDF file. You can also access these URLs from an API client like Postman or Insomnia.