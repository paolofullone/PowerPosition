using Axpo;
using PowerPosition.Worker;
using PowerPosition.Worker.Constants;
using PowerPosition.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<Worker>();

var outputFolder = builder.Configuration.GetValue<string>(PowerPositionConstants.settingsOutputFolder);

builder.Services.AddSingleton<IPowerPositionService, PowerPositionService>();
builder.Services.AddSingleton<IPowerService, PowerService>();

builder.Services.AddSingleton(sp => outputFolder);

var host = builder.Build();
host.Run();
