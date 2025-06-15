using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PowerPosition.Worker.Configuration;
using PowerPosition.Worker.Services.CsvGenerator;

namespace Unit.Services;

[Trait("Category", "CsvGenerator")]
public class CsvGeneratorServiceTests : IDisposable
{
    private readonly Mock<ILogger<CsvGeneratorService>> _mockLogger;
    private readonly CsvGeneratorService _sut;
    private readonly string _testOutputFolder = Path.Combine(Path.GetTempPath(), "PowerPositionTestsCsvGenerator");

    public CsvGeneratorServiceTests()
    {
        _mockLogger = new Mock<ILogger<CsvGeneratorService>>();

        var settings = Options.Create(new PowerPositionSettings
        {
            LocalTimeZone = "Europe/London"
        });

        Directory.CreateDirectory(_testOutputFolder);
        _sut = new CsvGeneratorService(settings, _mockLogger.Object);
    }

    [Fact]
    public async Task GenerateCsvAsync_ShouldCreateCorrectFormat_ForNormalDay()
    {
        var normalDate = new DateTime(2025, 1, 15, 10, 0, 0);
        var volumes = CreateTestVolumes(100);

        await _sut.GenerateCsvAsync(normalDate, volumes, _testOutputFolder, CancellationToken.None);

        var filePath = Path.Combine(_testOutputFolder, $"PowerPosition_{normalDate:yyyyMMdd_HHmm}.csv");
        var content = await File.ReadAllTextAsync(filePath);

        Assert.Contains("Local Time,Volume", content);
        Assert.Contains("23:00,100", content);
        Assert.Contains("00:00,200", content);
        Assert.Contains("01:00,300", content);
    }

    [Fact]
    public async Task GenerateCsvAsync_ShouldHandleSpringForward_WithSkippedHour()
    {
        // https://www.timezoneconverter.com/cgi-bin/zoneinfo?tz=Europe/London
        // DST began on Sun 30-Mar-2025 at 01:00:00 A.M. when local clocks were set forward 1 hour
        var springForwardDateTransition = new DateTime(2025, 3, 30, 1, 30, 0);
        var hourAfterTransitionMarker = "03:00";
        var volumeSecondHour = 200;
        var volumeThirdHour = 300;
        var expectedCombinedVolume = volumeSecondHour + volumeThirdHour;
        var hourAfterTransitionWithVolume = $"{hourAfterTransitionMarker},{expectedCombinedVolume}";
        var volumes = CreateTestVolumes(100);

        await _sut.GenerateCsvAsync(springForwardDateTransition, volumes, _testOutputFolder, CancellationToken.None);

        var filePath = Path.Combine(_testOutputFolder, $"PowerPosition_{springForwardDateTransition:yyyyMMdd_HHmm}.csv");
        var content = await File.ReadAllTextAsync(filePath);
        var lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

        var skippedHour = "01:00,";
        var hourAfterTransition = hourAfterTransitionWithVolume;

        Assert.DoesNotContain(lines, line => line.Contains(skippedHour));
        Assert.Contains(hourAfterTransition, lines);
    }

    [Fact]
    public async Task GenerateCsvAsync_ShouldHandleFallBack_WithDuplicateHour()
    {
        // https://www.timezoneconverter.com/cgi-bin/zoneinfo?tz=Europe/London
        // DST will end on Sun 26-Oct-2025 at 02:00:00 A.M. when local clocks are set backward 1 hour
        var fallBackDate = new DateTime(2025, 10, 26, 1, 30, 0);
        var volumes = CreateTestVolumes(1);

        await _sut.GenerateCsvAsync(fallBackDate, volumes, _testOutputFolder, CancellationToken.None);

        var filePath = Path.Combine(_testOutputFolder, $"PowerPosition_{fallBackDate:yyyyMMdd_HHmm}.csv");
        var content = await File.ReadAllTextAsync(filePath);
        var lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

        var duplicateHourMarker = "01:00";
        var duplicateHourLines = lines.Where(line => line.Contains(duplicateHourMarker)).ToList();

        var standardTimeEntry = "01:00,1.5";
        var daylightTimeEntry = "01:00 (DST),1.5";

        Assert.Equal(2, duplicateHourLines.Count);
        Assert.Contains(standardTimeEntry, duplicateHourLines);
        Assert.Contains(daylightTimeEntry, duplicateHourLines);

        var expectedLines = new[]
        {
        "Local Time,Volume",
        "23:00,1",
        "00:00,2",
        standardTimeEntry,
        daylightTimeEntry,
        "02:00,4",
        "03:00,5",
        "04:00,6",
        "05:00,7",
        "06:00,8",
        "07:00,9",
        "08:00,10",
        "09:00,11",
        "10:00,12",
        "11:00,13",
        "12:00,14",
        "13:00,15",
        "14:00,16",
        "15:00,17",
        "16:00,18",
        "17:00,19",
        "18:00,20",
        "19:00,21",
        "20:00,22",
        "21:00,23",
        "22:00,24"
    };

        Assert.Equal(expectedLines, lines);
    }

    [Fact]
    public async Task GenerateCsvAsync_ShouldHandleSummerTime_WithDSTActive()
    {
        var summerDate = new DateTime(2025, 6, 15, 10, 0, 0);
        var volumes = CreateTestVolumes(100);

        await _sut.GenerateCsvAsync(summerDate, volumes, _testOutputFolder, CancellationToken.None);

        var filePath = Path.Combine(_testOutputFolder, $"PowerPosition_{summerDate:yyyyMMdd_HHmm}.csv");
        var content = await File.ReadAllTextAsync(filePath);

        Assert.Contains("Local Time,Volume", content);

        Assert.Contains("23:00,100", content);
        Assert.Contains("00:00,200", content);
        Assert.Contains("01:00,300", content);
        Assert.Contains("02:00,400", content);
        Assert.Contains("03:00,500", content);
        Assert.Contains("04:00,600", content);
        Assert.Contains("05:00,700", content);
        Assert.Contains("06:00,800", content);
        Assert.Contains("07:00,900", content);
        Assert.Contains("08:00,1000", content);
        Assert.Contains("09:00,1100", content);
        Assert.Contains("10:00,1200", content);
        Assert.Contains("11:00,1300", content);
        Assert.Contains("12:00,1400", content);
        Assert.Contains("13:00,1500", content);
        Assert.Contains("14:00,1600", content);
        Assert.Contains("15:00,1700", content);
        Assert.Contains("16:00,1800", content);
        Assert.Contains("17:00,1900", content);
        Assert.Contains("18:00,2000", content);
        Assert.Contains("19:00,2100", content);
        Assert.Contains("20:00,2200", content);
        Assert.Contains("21:00,2300", content);
        Assert.Contains("22:00,2400", content);

        Assert.DoesNotContain("(DST)", content);
    }

    private static double[] CreateTestVolumes(int multiplier)
    {
        return Enumerable.Range(1, 24)
            .Select(i => (double)i * multiplier)
            .ToArray();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testOutputFolder))
        {
            Directory.Delete(_testOutputFolder, true);
        }
    }
}