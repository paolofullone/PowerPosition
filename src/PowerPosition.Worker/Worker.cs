using PowerPosition.Worker.Constants;
using PowerPosition.Worker.Services;

namespace PowerPosition.Worker
{
    public class Worker : BackgroundService
    {
        private readonly IPowerPositionService _service;
        private readonly ILogger<Worker> _logger;
        private readonly TimeSpan _interval;
        private readonly TimeZoneInfo _londonTimeZone;
        private readonly DateTime _utcNow;
        private readonly DateTime _londonNow;

        public Worker(IPowerPositionService service, ILogger<Worker> logger, IConfiguration configuration)
        {
            _service = service;
            _logger = logger;
            _londonTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
            _utcNow = DateTime.UtcNow;
            _londonNow = TimeZoneInfo.ConvertTimeFromUtc(_utcNow, _londonTimeZone);
            _interval = TimeSpan.FromMinutes(configuration.GetValue<int>(PowerPositionConstants.settingsInterval));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Using interval: {IntervalMinutes} minute(s) starting execute at Utc: {UtcTime} (Europe/London: {LocalTime})",
                _interval.TotalMinutes, _utcNow, _londonNow
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
