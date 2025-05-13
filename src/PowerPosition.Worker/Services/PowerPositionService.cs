using Axpo;

namespace PowerPosition.Worker.Services;

public class PowerPositionService : IPowerPositionService
{
    private readonly IPowerService _powerService;
    private readonly ILogger<PowerPositionService> _logger;
    private readonly string _outputFolder;

    public PowerPositionService(IPowerService powerService, ILogger<PowerPositionService> logger, string outputFolder)
    {
        _powerService = powerService;
        _logger = logger;
        _outputFolder = outputFolder;
    }

    public async Task GenerateReportAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
