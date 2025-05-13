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
                "Using interval: {IntervalMinutes} minute(s) at Utc: {UtcTime} (Local: {LocalTime})",
                _interval.TotalMinutes, _utcNow, _londonNow
                );

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Power Worker running with interval of {interval} minute(s)", _interval.TotalMinutes);
                await _service.GenerateReportAsync(stoppingToken);
            }
        }
    }
}
