using PuppeteerSharp;

namespace PupperteersharpPdfReportSample.Services;

public class BrowserSetupService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var browserFetcher = new BrowserFetcher();
        Console.WriteLine("Downloading browser binaries for PuppeteerSharp...");
        await browserFetcher.DownloadAsync();
        Console.WriteLine("Browser binaries downloaded successfully.");
    }
}
