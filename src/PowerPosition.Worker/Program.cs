using Axpo;
using PowerPosition.Worker;
using PowerPosition.Worker.Configuration;
using PowerPosition.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<Worker>();

builder.Services.Configure<PowerPositionSettings>(builder.Configuration.GetSection("PowerPositionSettings"));

builder.Services.AddSingleton<IPowerPositionService, PowerPositionService>();
builder.Services.AddSingleton<IPowerService, PowerService>();


var host = builder.Build();
host.Run();
