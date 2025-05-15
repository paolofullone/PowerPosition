using Axpo;
using PowerPosition.Worker;
using PowerPosition.Worker.Configuration;
using PowerPosition.Worker.Services;
using Serilog;
using Serilog.Formatting.Compact;

var logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateLogger();

Log.Logger = logger;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

builder.Services.AddHostedService<Worker>();

builder.Services.Configure<PowerPositionSettings>(builder.Configuration.GetSection("PowerPositionSettings"));

builder.Services.AddSingleton<IPowerPositionService, PowerPositionService>();
builder.Services.AddSingleton<IPowerService, PowerService>();

var host = builder.Build();
host.Run();
