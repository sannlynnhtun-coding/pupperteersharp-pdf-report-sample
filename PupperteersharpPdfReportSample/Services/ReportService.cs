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
