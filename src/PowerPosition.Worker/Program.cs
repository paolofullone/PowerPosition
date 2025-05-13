using Axpo;
using PowerPosition.Worker;
using PowerPosition.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

var outputFolder = builder.Configuration.GetValue<string>("PowerPosition:OutputFolder");

builder.Services.AddSingleton<IPowerPositionService, PowerPositionService>();
builder.Services.AddSingleton<IPowerService, PowerService>();

builder.Services.AddSingleton(sp =>
    builder.Configuration.GetValue<string>("PowerPosition:OutputFolder"));


var host = builder.Build();
host.Run();
