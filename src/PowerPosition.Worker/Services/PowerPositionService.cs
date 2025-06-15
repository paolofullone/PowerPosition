using Microsoft.Extensions.Options;
using PowerPosition.Worker.Configuration;
using PowerPosition.Worker.Services.CsvGenerator;
using PowerPosition.Worker.Services.TradeFetcher;
using PowerPosition.Worker.Services.VolumeCalculator;

namespace PowerPosition.Worker.Services;

public class PowerPositionService(
    ITradeService tradeFetcher,
    IVolumeCalculatorService volumeCalculator,
    ICsvGeneratorService csvGenerator,
    ILogger<PowerPositionService> logger,
    IOptions<PowerPositionSettings> settings) : IPowerPositionService
{
    private readonly string _outputFolder = EnsureDirectory(settings.Value.OutputFolder);
    private readonly TimeZoneInfo _localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.Value.LocalTimeZone);

    public async Task GenerateReportAsync(CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, _localTimeZone);

        logger.LogInformation("Generating power report for {LocalTime}", localNow);

        var trades = await tradeFetcher.GetTradesAsync(localNow.Date, cancellationToken);
        var volumes = volumeCalculator.AggregateVolumes(trades);
        await csvGenerator.GenerateCsvAsync(localNow, volumes, _outputFolder, cancellationToken);
    }

    private static string EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}