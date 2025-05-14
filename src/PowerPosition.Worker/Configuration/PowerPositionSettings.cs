namespace PowerPosition.Worker.Configuration;

public class PowerPositionSettings
{
    public string OutputFolder { get; set; } = string.Empty;
    public string LocalTimeZone { get; set; } = string.Empty;
    public int IntervalInMinutes { get; set; }
}
