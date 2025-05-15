using Axpo;
using Microsoft.Extensions.Options;
using PowerPosition.Worker.Configuration;
using System.Text;

namespace PowerPosition.Worker.Services;

public class PowerPositionService(
    IPowerService powerService,
    ILogger<PowerPositionService> logger,
    IOptions<PowerPositionSettings> settings
    ) : IPowerPositionService
{
    private static string EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }

    private readonly string _outputFolder = EnsureDirectory(settings.Value.OutputFolder);
    private readonly TimeZoneInfo _localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.Value.LocalTimeZone);
    private readonly string _localTimeZoneId = settings.Value.LocalTimeZone;
    private readonly int _retryDelayMs = settings.Value.RetryDelayMillisecods;

    /// <summary>
    /// Generates the power position report and writes it to a CSV file.
    /// </summary>
    public async Task GenerateReportAsync(CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, _localTimeZone);
        var localReportDate = localNow.Date;

        logger.LogInformation(
            "Generate Power Report request for date {localReportDate} started at Utc: {utcNow} ({LocalZone}: {LocalTime})",
            localReportDate, utcNow, _localTimeZoneId, localNow);


        IEnumerable<PowerTrade> trades;
        try
        {
            trades = await ResilientGetTradesAsync(localReportDate, cancellationToken);
            var aggregatedVolumes = AggregateVolumes(trades);
            await WriteCsvAsync(localNow, aggregatedVolumes, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogError("Power Report request canceled for date {ReportDate}",
                localReportDate);
        }
    }

    private static double[] AggregateVolumes(IEnumerable<PowerTrade> trades)
    {
        var volumes = new double[24];
        foreach (var trade in trades)
        {
            foreach (var period in trade.Periods)
            {
                int periodIndex = period.Period - 1;
                if (periodIndex >= 0 && periodIndex <= 23)
                {
                    volumes[periodIndex] += period.Volume;
                }
            }
        }
        return volumes;
    }

    private async Task<IEnumerable<PowerTrade>> ResilientGetTradesAsync(DateTime date, CancellationToken cancellationToken)
    {
        int retryCount = 1;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                return await powerService.GetTradesAsync(date);
            }
            catch (PowerServiceException ex)
            {

                logger.LogWarning(ex, "Retry {RetryCount} for Power Report request at date {date}. Message: {ErrorMessage}",
                    retryCount, date, ex.Message);
                await Task.Delay(TimeSpan.FromMilliseconds(_retryDelayMs), cancellationToken);
                retryCount++;
            }
        }
        throw new OperationCanceledException(cancellationToken);
    }

    private async Task WriteCsvAsync(DateTime localNow, double[] volumes, CancellationToken cancellationToken)
    {
        var fileName = $"PowerPosition_{localNow:yyyyMMdd_HHmm}.csv";
        var filePath = Path.Combine(_outputFolder, fileName);
        var csvContent = BuildCsvContent(volumes);

        await File.WriteAllTextAsync(filePath, csvContent, cancellationToken);

        logger.LogInformation(
            "Power Report Extract completed, CSV written to {FilePath} at {UtcTime} (Local: {LocalTime})",
            filePath, DateTime.UtcNow, TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _localTimeZone));
    }

    private static string BuildCsvContent(double[] volumes)
    {
        var delimiter = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine($"Local Time{delimiter}Volume");

        for (int period = 1; period <= 24; period++)
        {
            var hour = (period == 1) ? 23 : (period - 2);
            var time = $"{hour}:00";
            var volume = Math.Round(volumes[period - 1], 2);
            stringBuilder.AppendLine($"{time}{delimiter}{volume}");
        }

        return stringBuilder.ToString();
    }
}
