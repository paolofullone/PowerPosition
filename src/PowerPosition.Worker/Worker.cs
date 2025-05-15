using Microsoft.Extensions.Options;
using PowerPosition.Worker.Configuration;
using PowerPosition.Worker.Services;

namespace PowerPosition.Worker
{
    public class Worker(
        IPowerPositionService service,
        ILogger<Worker> logger,
        IOptions<PowerPositionSettings> settings
        ) : BackgroundService
    {

        private readonly TimeSpan _interval = TimeSpan.FromMinutes(settings.Value.IntervalInMinutes);
        private readonly TimeZoneInfo _localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.Value.LocalTimeZone);
        private readonly string _localTimeZoneId = settings.Value.LocalTimeZone;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var utcNow = DateTime.UtcNow;
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, _localTimeZone);
            logger.LogInformation(
                "Using interval: {IntervalMinutes} minute(s) starting execute at Utc: {UtcTime} ({LocalZone}: {LocalTime})",
                _interval.TotalMinutes, utcNow, _localTimeZoneId, localNow
                );

            await service.GenerateReportAsync(stoppingToken);

            using PeriodicTimer timer = await ScheduledExecutions(stoppingToken);
        }

        private async Task<PeriodicTimer> ScheduledExecutions(CancellationToken stoppingToken)
        {
            var timer = new PeriodicTimer(_interval);
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await service.GenerateReportAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error during extract");
                }
            }

            return timer;
        }
    }
}
