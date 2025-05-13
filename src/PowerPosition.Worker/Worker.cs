using PowerPosition.Worker.Services;

namespace PowerPosition.Worker
{
    public class Worker : BackgroundService
    {
        private readonly IPowerPositionService _service;
        private readonly ILogger<Worker> _logger;
        private readonly TimeSpan _interval;

        public Worker(IPowerPositionService service, ILogger<Worker> logger, IConfiguration configuration)
        {
            _service = service;
            _logger = logger;
            _interval = TimeSpan.FromMinutes(configuration.GetValue<int>("PowerPosition:IntervalInMinutes"));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Power Worker running with interval of {interval} minute(s)", _interval.TotalMinutes);
                await _service.GenerateReportAsync(stoppingToken);
            }
        }
    }
}
