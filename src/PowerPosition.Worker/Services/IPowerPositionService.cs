namespace PowerPosition.Worker.Services;

public interface IPowerPositionService
{
    Task GenerateReportAsync(CancellationToken cancellationToken);
}