using EmailClassifier;
using EmailClassifier.Data;
using EmailClassifier.Models;
using EmailClassifier.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((services, lc) => lc.ReadFrom.Configuration(builder.Configuration));

var dbPassword =
    Environment.GetEnvironmentVariable("DEV_DB_PASSWORD")
    ?? throw new InvalidOperationException("DEV_DB_PASSWORD environment variable is not set.");

var connectionString =
    $"Host=localhost;Port=5433;Database=devdb;Username=devuser;Password={dbPassword}";

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection("Worker"));

builder.Services.AddSingleton<WatermarkService>();
builder.Services.AddSingleton<IEmailClassifierService, OpenAiEmailClassifierService>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
