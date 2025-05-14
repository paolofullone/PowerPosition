using Axpo;
using Microsoft.Extensions.Options;
using PowerPosition.Worker.Configuration;
using PowerPosition.Worker.Constants;
using System.Text;

namespace PowerPosition.Worker.Services;

public class PowerPositionService : IPowerPositionService
{
    private readonly IPowerService _powerService;
    private readonly ILogger<PowerPositionService> _logger;
    IOptions<PowerPositionSettings> _settings;
    private readonly string _outputFolder;
    private readonly TimeZoneInfo _localTimeZone;

    public PowerPositionService(IPowerService powerService, ILogger<PowerPositionService> logger, IOptions<PowerPositionSettings> settings)
    {
        _powerService = powerService;
        _logger = logger;
        _outputFolder = settings.Value.OutputFolder;
        _settings = settings;
        _localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.Value.LocalTimeZone);
        Directory.CreateDirectory(_outputFolder);
    }

    public async Task GenerateReportAsync(CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, _localTimeZone);

        _logger.LogInformation(
            "Generate report async at Utc: {UtcTime} ({LocalZone}: {LocalTime})",
            utcNow, _settings.Value.LocalTimeZone, localNow);

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
            _logger.LogError(ex, "PowerService failed for date {ReportDate}. Message: {ErrorMessage}",
                localReportDate, ex.Message);
            return;
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
                return await _powerService.GetTradesAsync(date);
            }
            catch (PowerServiceException ex)
            {
                double retryDelayMs = PowerPositionConstants.retryDelayMiliseconds * Math.Pow(2, retryCount - 1);
                _logger.LogWarning(ex, "Retry {RetryCount} for PowerServiceException after {RetryDelayMs}ms. Message: {ErrorMessage}",
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

        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine("Local Time, Volume");

        for (int period = 1; period <= 24; period++)
        {
            var hour = (period == 1) ? 23 : (period - 2);
            var time = $"{hour}:00";
            var volume = volumes[period - 1];
            stringBuilder.AppendLine($"{time},{volume}");
        }

        await File.WriteAllTextAsync(filePath, stringBuilder.ToString(), cancellationToken);

        _logger.LogInformation(
            "Extract completed, CSV written to {FilePath} at {UtcTime} (Local: {LocalTime})",
            filePath, DateTime.UtcNow, TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _localTimeZone));
    }
}
