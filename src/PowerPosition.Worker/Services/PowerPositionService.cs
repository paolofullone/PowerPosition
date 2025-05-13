using Axpo;
using PowerPosition.Worker.Constants;

namespace PowerPosition.Worker.Services;

public class PowerPositionService : IPowerPositionService
{
    private readonly IPowerService _powerService;
    private readonly ILogger<PowerPositionService> _logger;
    private readonly string _outputFolder;
    private readonly TimeZoneInfo _londonTimeZone;

    public PowerPositionService(IPowerService powerService, ILogger<PowerPositionService> logger, string outputFolder)
    {
        _powerService = powerService;
        _logger = logger;
        _outputFolder = outputFolder;
        _londonTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
    }

    public async Task GenerateReportAsync(CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var londonNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, _londonTimeZone);

        _logger.LogInformation(
            "Generate report async at Utc: {UtcTime} (Europe/London: {LocalTime}",
            utcNow, londonNow);

        var londonReportDate = londonNow.Date;

        IEnumerable<PowerTrade> trades;
        try
        {
            trades = await ResilientGetTradesAsync(londonReportDate, cancellationToken);
        }
        catch (PowerServiceException ex)
        {
            _logger.LogError(ex, "PowerService failed for date {ReportDate}. Message: {ErrorMessage}",
                londonReportDate, ex.Message);
            return;
        }

        var aggregatedVolumes = AggregateVolumes(trades);
    }

    private static double[] AggregateVolumes(IEnumerable<PowerTrade> trades)
    {
        var volumes = new double[24];
        foreach (var trade in trades)
        {
            foreach (var period in trade.Periods)
            {
                int periodIndex = period.Period - 1;
                if (periodIndex >= 0 && periodIndex < 24)
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
}
