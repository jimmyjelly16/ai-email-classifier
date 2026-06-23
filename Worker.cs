using EmailClassifier.Data;
using EmailClassifier.Models;
using EmailClassifier.Services;
using Microsoft.Extensions.Options;

namespace EmailClassifier;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WorkerOptions _options;

    public Worker(
        ILogger<Worker> logger,
        IServiceScopeFactory scopeFactory,
        IOptions<WorkerOptions> options
    )
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Email Classifier Worker started. Interval={Interval}s",
            _options.IntervalSeconds
        );

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker tick at {Time}", DateTimeOffset.UtcNow);

            try
            {
                using var scope = _scopeFactory.CreateScope();

                var processingService = new EmailProcessingService(
                    scope.ServiceProvider.GetRequiredService<AppDbContext>(),
                    scope.ServiceProvider.GetRequiredService<IEmailClassifierService>(),
                    scope.ServiceProvider.GetRequiredService<WatermarkService>(),
                    scope.ServiceProvider.GetRequiredService<IOptions<WorkerOptions>>(),
                    scope.ServiceProvider.GetRequiredService<ILogger<EmailProcessingService>>()
                );

                await processingService.ProcessPendingEmailsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in Worker tick.");
            }

            _logger.LogInformation("Worker sleeping for {Interval}s", _options.IntervalSeconds);

            await Task.Delay(TimeSpan.FromSeconds(_options.IntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("Email Classifier Worker stopped.");
    }
}
