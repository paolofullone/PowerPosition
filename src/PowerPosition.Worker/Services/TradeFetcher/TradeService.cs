using Axpo;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using PowerPosition.Worker.Configuration;

namespace PowerPosition.Worker.Services.TradeFetcher;

public class TradeService : ITradeService
{
    private readonly IPowerService _powerService;
    private readonly ILogger<TradeService> _logger;
    private readonly IOptions<PowerPositionSettings> _settings;
    private readonly AsyncRetryPolicy<IEnumerable<PowerTrade>> _retryPolicy;

    public TradeService(
        IPowerService powerService,
        IOptions<PowerPositionSettings> settings,
        ILogger<TradeService> logger)
    {
        _powerService = powerService;
        _settings = settings;
        _logger = logger;

        _retryPolicy = Policy<IEnumerable<PowerTrade>>
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: settings.Value.MaxRetryAttempts,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromMilliseconds(
                        settings.Value.InitialRetryDelayMilliseconds * Math.Pow(2, attempt - 1)
                    ),
                onRetry: (exception, delay, attempt, context) =>
                {
                    _logger.LogWarning(
                        "Retry attempt {Attempt}/{MaxAttempts}. Next retry in {Delay}ms. Error: {ErrorMessage}",
                        attempt,
                        settings.Value.MaxRetryAttempts,
                        delay.TotalMilliseconds,
                        exception.Exception.Message);
                });
    }

    public async Task<IEnumerable<PowerTrade>> GetTradesAsync(DateTime date, CancellationToken cancellationToken)
    {
        try
        {
            return await _retryPolicy.ExecuteAsync(async ct =>
            {
                ct.ThrowIfCancellationRequested();
                return await _powerService.GetTradesAsync(date);
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to fetch trades after {MaxAttempts} attempts",
                _settings.Value.MaxRetryAttempts);
            throw;
        }
    }
}