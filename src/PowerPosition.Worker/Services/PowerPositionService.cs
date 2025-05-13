using Axpo;

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
            trades = await _powerService.GetTradesAsync(londonReportDate);
            _logger.LogInformation("Trades count for date {londonReportDate}: {tradesCount}", londonReportDate, trades.Count());
        }
        catch (PowerServiceException ex)
        {
            _logger.LogError(ex, "PowerService failed for date {ReportDate}. Message: {ErrorMessage}",
                londonReportDate, ex.Message);
            return;
        }
    }
}
