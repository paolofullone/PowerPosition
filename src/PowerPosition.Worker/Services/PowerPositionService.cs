using Axpo;
using Microsoft.Extensions.Options;
using PowerPosition.Worker.Configuration;
using PowerPosition.Worker.Constants;
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

    /// <summary>
    /// Generates the power position report and writes it to a CSV file.
    /// </summary>
    public async Task GenerateReportAsync(CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, _localTimeZone);

        logger.LogInformation(
            "Generate report async at Utc: {UtcTime} ({LocalZone}: {LocalTime})",
            utcNow, _localTimeZoneId, localNow);

        var localReportDate = localNow.Date;

        IEnumerable<PowerTrade> trades;
        try
        {
            trades = await ResilientGetTradesAsync(localReportDate, cancellationToken);
            var aggregatedVolumes = AggregateVolumes(trades);
            await WriteCsvAsync(localNow, aggregatedVolumes, cancellationToken);
        }
        catch (PowerServiceException ex)
        {
            logger.LogError(ex, "PowerService failed for date {ReportDate}. Message: {ErrorMessage}",
                localReportDate, ex.Message);
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
        while (retryCount <= PowerPositionConstants.maxGetTradesRetries && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                return await powerService.GetTradesAsync(date);
            }
            catch (PowerServiceException ex)
            {
                double retryDelayMs = PowerPositionConstants.retryDelayMiliseconds * Math.Pow(2, retryCount - 1);
                logger.LogWarning(ex, "Retry {RetryCount} for PowerServiceException after {RetryDelayMs}ms. Message: {ErrorMessage}",
                    retryCount, retryDelayMs, ex.Message);
                await Task.Delay(TimeSpan.FromMilliseconds(retryDelayMs), cancellationToken);
                retryCount++;
            }
        }
        string errorMessage = cancellationToken.IsCancellationRequested
            ? "Operation canceled after multiple retry attempts"
            : $"Max retries ({PowerPositionConstants.maxGetTradesRetries}) reached";
        throw new PowerServiceException(errorMessage);
    }

    private async Task WriteCsvAsync(DateTime localNow, double[] volumes, CancellationToken cancellationToken)
    {
        var fileName = $"PowerPosition_{localNow:yyyyMMdd_HHmm}.csv";
        var filePath = Path.Combine(_outputFolder, fileName);
        var csvContent = BuildCsvContent(volumes);

        await File.WriteAllTextAsync(filePath, csvContent, cancellationToken);

        logger.LogInformation(
            "Extract completed, CSV written to {FilePath} at {UtcTime} (Local: {LocalTime})",
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
