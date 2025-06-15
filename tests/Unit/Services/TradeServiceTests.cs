using Axpo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PowerPosition.Worker.Configuration;
using PowerPosition.Worker.Constants;
using PowerPosition.Worker.Services.TradeFetcher;

namespace Unit.Services;

[Trait("Category", "TradeFetcher")]
[Trait("Category", "TradeFetcher")]
public class TradeServiceTests : IDisposable
{
    private readonly Mock<IPowerService> _mockPowerService;
    private readonly Mock<ILogger<TradeService>> _mockLogger;
    private readonly string _testOutputFolder = Path.Combine(Path.GetTempPath(), "PowerPositionTestsTradeFetcher");
    private readonly TradeService _sut;

    public TradeServiceTests()
    {
        _mockPowerService = new Mock<IPowerService>();
        _mockLogger = new Mock<ILogger<TradeService>>();

        var settings = Options.Create(new PowerPositionSettings
        {
        });

        _sut = new TradeService(
            _mockPowerService.Object,
            settings,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetTradesAsync_ShouldReturnTrades_OnFirstAttempt()
    {
        // Arrange
        var testDate = DateTime.Today;
        var testTrade = CreateTestTrade(100);
        var expectedTrades = new List<PowerTrade> { testTrade };

        _mockPowerService
            .Setup(x => x.GetTradesAsync(testDate))
            .ReturnsAsync(expectedTrades);

        // Act
        var result = await _sut.GetTradesAsync(testDate, CancellationToken.None);

        // Assert
        Assert.Equal(expectedTrades, result);
        _mockPowerService.Verify(x => x.GetTradesAsync(testDate), Times.Once);
    }

    [Fact]
    public async Task GetTradesAsync_ShouldRetryAndSucceed_AfterTransientFailure()
    {
        // Arrange
        var testDate = DateTime.Today;
        var expectedTrades = new List<PowerTrade> { CreateTestTrade() };
        var attempt = 0;

        _mockPowerService
            .Setup(x => x.GetTradesAsync(testDate))
            .ReturnsAsync(() =>
            {
                if (attempt++ < 2)
                    throw new PowerServiceException("Simulated transient failure");
                return expectedTrades;
            });

        // Act
        var result = await _sut.GetTradesAsync(testDate, CancellationToken.None);

        // Assert
        Assert.Equal(expectedTrades, result);
        _mockPowerService.Verify(x => x.GetTradesAsync(testDate), Times.Exactly(3));
        VerifyLogWarning(2);
    }

    [Fact]
    public async Task GetTradesAsync_ShouldThrow_AfterMaxRetryAttempts()
    {
        // Arrange
        var testDate = DateTime.Today;
        var exception = new PowerServiceException("Persistent failure");

        _mockPowerService
            .Setup(x => x.GetTradesAsync(testDate))
            .ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsAsync<PowerServiceException>(() =>
            _sut.GetTradesAsync(testDate, CancellationToken.None));

        _mockPowerService.Verify(x => x.GetTradesAsync(testDate),
            Times.Exactly(PowerPositionConstants.MaxRetryAttempts + 1));
        VerifyLogError();
    }

    [Fact]
    public async Task GetTradesAsync_ShouldHandleDifferentExceptionTypes()
    {
        // Arrange
        var testDate = DateTime.Today;
        var expectedTrades = new List<PowerTrade> { CreateTestTrade() };
        var exceptions = new Exception[]
        {
            new IOException("IO failure"),
            new HttpRequestException("Network failure"),
            new PowerServiceException("Service failure")
        };
        var attempt = 0;

        _mockPowerService
            .Setup(x => x.GetTradesAsync(testDate))
            .ReturnsAsync(() =>
            {
                if (attempt < exceptions.Length)
                    throw exceptions[attempt++];
                return expectedTrades;
            });

        // Act
        var result = await _sut.GetTradesAsync(testDate, CancellationToken.None);

        // Assert
        Assert.Equal(expectedTrades, result);
        _mockPowerService.Verify(x => x.GetTradesAsync(testDate), Times.Exactly(4));
        VerifyLogWarning(3);
    }

    [Fact]
    public async Task GetTradesAsync_ShouldPropagateCancellation()
    {
        // Arrange
        var testDate = DateTime.Today;
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.GetTradesAsync(testDate, cts.Token));

        _mockPowerService.Verify(x => x.GetTradesAsync(It.IsAny<DateTime>()), Times.Never);
    }

    private static PowerTrade CreateTestTrade(double volume = 0, DateTime? date = null)
    {
        var tradeDate = date ?? DateTime.Today;
        var trade = PowerTrade.Create(tradeDate, 24);

        foreach (var period in trade.Periods)
        {
            period.SetVolume(volume);
        }

        return trade;
    }

    private void VerifyLogWarning(int times)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Exactly(times));
    }

    private void VerifyLogError()
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }
    public void Dispose()
    {
        if (Directory.Exists(_testOutputFolder))
        {
            Directory.Delete(_testOutputFolder, true);
        }
    }
}