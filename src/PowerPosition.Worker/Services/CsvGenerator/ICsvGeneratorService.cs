namespace PowerPosition.Worker.Services.CsvGenerator;

public interface ICsvGeneratorService
{
    Task GenerateCsvAsync(DateTime localNow, double[] volumes, string outputFolder, CancellationToken cancellationToken);
}