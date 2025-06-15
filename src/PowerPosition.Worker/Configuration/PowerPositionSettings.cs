using PowerPosition.Worker.Constants;

namespace PowerPosition.Worker.Configuration;

public class PowerPositionSettings
{
    public string OutputFolder { get; set; } = PowerPositionConstants.OutputFolder;
    public int IntervalInMinutes { get; set; } = PowerPositionConstants.IntervalInMinutes;
    public string LocalTimeZone { get; set; } = PowerPositionConstants.localTimeZone;
    public int InitialRetryDelayMilliseconds { get; set; } = PowerPositionConstants.MaxRetryAttempts;
    public int MaxRetryAttempts { get; set; } = PowerPositionConstants.MaxRetryAttempts;
}
