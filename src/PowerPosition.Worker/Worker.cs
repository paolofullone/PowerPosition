using PowerPosition.Worker.Services;

namespace PowerPosition.Worker
{
    public class Worker : BackgroundService
    {
        private readonly IPowerPositionService _service;
        private readonly ILogger<Worker> _logger;

        public Worker(IPowerPositionService service, ILogger<Worker> logger)
        {
            _service = service;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _service.GenerateReportAsync(stoppingToken);
            }
        }
    }
}
