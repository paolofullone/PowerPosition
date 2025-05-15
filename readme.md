# Power Position Report Generator

This project is a background service built in C# that generates a Power Position Report.
The trades are fetched from a PowerService.dll library.

## Report

The result is a CSV file with the intra-day trades with a final volume of all trades per hour.

As per the defined requirements, the report starts at 23:00 of the previous day and finishes at 22:00 of the current day.

## Configuration

In the `appsettings.json` file we can configure:
- OutputFolder: The folder where the report will be saved.
- IntervalInMinutes: The interval in minutes at which the report will be generated.
- LocalTimeZone: The time zone to be used for the report.
- RetryDelayMilliseconds: The delay in milliseconds before retrying to generate the report if an error occurs in PowerService.dll lib.


```json
  "PowerPositionSettings": {
    "OutputFolder": "../../reports",
    "IntervalInMinutes": 1,
    "LocalTimeZone": "Europe/London",
    "RetryDelayMilliseconds":  200
  }
  ```

  If no Interval is specified, the report will be generated every 60 minutes.
  If no RetryDelay is specified, the delay will be 500 milliseconds.

  Both of this default values can be changed in the `PowerPositionSettings` class:

  ```csharp
  public class PowerPositionSettings
{
    public string OutputFolder { get; set; } = string.Empty;
    public string LocalTimeZone { get; set; } = string.Empty;
    public int IntervalInMinutes { get; set; } = PowerPositionConstants.defaultIntervalInMinutes;
    public int RetryDelayMilliseconds { get; set; } = PowerPositionConstants.defaultDelayMiliseconds;
}
```

## Usage

Clone the project:
```bash
git clone https://github.com/paolofullone/PowerPosition.git
```

Enter the project folder:
```bash
cd PowerPosition
cd src
cd PowerPosition.Worker
```

Run the project
```bash
dotnet run
```

## Logs

Logs are configured in `Program.cs` with Serilog (Json format):

```csharp
var logger = new LoggerConfiguration()
   .Enrich.FromLogContext()
   .WriteTo.Console(new CompactJsonFormatter())
   .CreateLogger();

Log.Logger = logger;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);
```

If necessary we can remove this configuration to improve readability in console, the intent of having a Json format is to be able to ingest it in Datadog, Dynatrace or any other APM tool.

```csharp
var logger = new LoggerConfiguration()
   .Enrich.FromLogContext()
   .WriteTo.Console(new CompactJsonFormatter())
   .CreateLogger();

Log.Logger = logger;

var builder = Host.CreateApplicationBuilder(args);

// builder.Logging.ClearProviders();
// builder.Logging.AddSerilog(logger);
```