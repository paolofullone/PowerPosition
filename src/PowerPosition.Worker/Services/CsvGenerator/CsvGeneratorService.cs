using Microsoft.Extensions.Options;
using PowerPosition.Worker.Configuration;
using System.Globalization;
using System.Text;

namespace PowerPosition.Worker.Services.CsvGenerator;
public class CsvGeneratorService : ICsvGeneratorService
{
    private readonly TimeZoneInfo _localTimeZone;
    private readonly ILogger<CsvGeneratorService> _logger;

    public CsvGeneratorService(IOptions<PowerPositionSettings> settings, ILogger<CsvGeneratorService> logger)
    {
        _localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.Value.LocalTimeZone);
        _logger = logger;
    }

    public async Task GenerateCsvAsync(DateTime localNow, double[] volumes, string outputFolder, CancellationToken cancellationToken)
    {
        var fileName = $"PowerPosition_{localNow:yyyyMMdd_HHmm}.csv";
        var filePath = Path.Combine(outputFolder, fileName);
        var csvContent = BuildCsvContent(localNow, volumes);

        await File.WriteAllTextAsync(filePath, csvContent, cancellationToken);
        _logger.LogInformation("CSV generated at {FilePath}", filePath);
    }

    private string BuildCsvContent(DateTime localNow, double[] volumes)
    {
        var delimiter = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
        var csvContent = new StringBuilder();
        csvContent.AppendLine($"Local Time{delimiter}Volume");

        var reportDate = localNow.Date;
        var volumeFromSkippedHour = 0.0;

        for (int period = 1; period <= 24; period++)
        {
            var hour = (period == 1) ? 23 : (period - 2);
            var hourlyTimestamp = new DateTime(reportDate.Year, reportDate.Month, reportDate.Day, hour, 0, 0);

            if (_localTimeZone.IsInvalidTime(hourlyTimestamp))
            {
                volumeFromSkippedHour += HandleSkippedHour(volumes, period);
                continue;
            }

            if (_localTimeZone.IsAmbiguousTime(hourlyTimestamp))
            {
                HandleDuplicateHour(hourlyTimestamp, volumes[period - 1], csvContent, delimiter);
                continue;
            }

            HandleNormalHour(hourlyTimestamp, volumes[period - 1], ref volumeFromSkippedHour, csvContent, delimiter);
        }

        HandleRemainingVolume(volumeFromSkippedHour, csvContent, delimiter);
        return csvContent.ToString();
    }

    private double HandleSkippedHour(double[] volumes, int period)
    {
        return volumes[period - 1];
    }

    private void HandleDuplicateHour(
        DateTime hourlyTimestamp,
        double volume,
        StringBuilder csvContent,
        string delimiter)
    {
        var offsets = _localTimeZone.GetAmbiguousTimeOffsets(hourlyTimestamp)
            .OrderByDescending(offset => offset)
            .ToList();

        foreach (var offset in offsets)
        {
            var isDaylightTime = offset == offsets.Last();
            var hourLabel = $"{hourlyTimestamp:HH:mm}{(isDaylightTime ? " (DST)" : "")}";
            var splitVolume = Math.Round(volume / offsets.Count, 2);
            csvContent.AppendLine($"{hourLabel}{delimiter}{splitVolume}");
        }
    }

    private void HandleNormalHour(
        DateTime hourlyTimestamp,
        double volume,
        ref double volumeFromSkippedHour,
        StringBuilder csvContent,
        string delimiter)
    {
        var totalVolume = volume + volumeFromSkippedHour;
        csvContent.AppendLine($"{hourlyTimestamp:HH:mm}{delimiter}{Math.Round(totalVolume, 2)}");
        volumeFromSkippedHour = 0;
    }

    private void HandleRemainingVolume(
        double remainingVolume,
        StringBuilder csvContent,
        string delimiter)
    {
        if (remainingVolume > 0)
        {
            csvContent.AppendLine($"23:59{delimiter}{Math.Round(remainingVolume, 2)} (Carried forward)");
        }
    }
}
