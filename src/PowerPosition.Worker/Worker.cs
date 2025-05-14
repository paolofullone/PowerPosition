using Microsoft.Extensions.Options;
using PowerPosition.Worker.Configuration;
using PowerPosition.Worker.Services;

namespace PowerPosition.Worker
{
    public class Worker : BackgroundService
    {
        private readonly IPowerPositionService _service;
        private readonly ILogger<Worker> _logger;
        IOptions<PowerPositionSettings> _settings;
        private readonly TimeSpan _interval;
        private readonly TimeZoneInfo _localTimeZone;
        private readonly DateTime _utcNow;
        private readonly DateTime _localNow;

        public Worker(IPowerPositionService service, ILogger<Worker> logger, IOptions<PowerPositionSettings> settings, IConfiguration configuration)
        {
            _service = service;
            _logger = logger;
            _settings = settings;
            _localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.Value.LocalTimeZone);
            _utcNow = DateTime.UtcNow;
            _localNow = TimeZoneInfo.ConvertTimeFromUtc(_utcNow, _localTimeZone);
            _interval = TimeSpan.FromMinutes(settings.Value.IntervalInMinutes);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Using interval: {IntervalMinutes} minute(s) starting execute at Utc: {UtcTime} ({LocalZone}: {LocalTime})",
                _interval.TotalMinutes, _utcNow, _settings.Value.LocalTimeZone, _localNow
                );

            await _service.GenerateReportAsync(stoppingToken);

            using PeriodicTimer timer = await ScheduledExecutions(stoppingToken);
        }

        private async Task<PeriodicTimer> ScheduledExecutions(CancellationToken stoppingToken)
        {
            var timer = new PeriodicTimer(_interval);
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await _service.GenerateReportAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error during extract");
                }
            }

            return timer;
        }
    }
}
