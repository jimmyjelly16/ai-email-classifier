using System.Globalization;
using EmailClassifier.Data;
using EmailClassifier.Models;
using Microsoft.EntityFrameworkCore;

namespace EmailClassifier.Services;

public class WatermarkService
{
    private const string WatermarkKey = "EmailProcessor.Watermark";
    private readonly ILogger<WatermarkService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public WatermarkService(IServiceScopeFactory scopeFactory, ILogger<WatermarkService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<DateTime?> GetWatermarkAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var state = await db.WorkerStates.FirstOrDefaultAsync(w => w.Key == WatermarkKey);

        if (state == null)
        {
            _logger.LogInformation("No watermark found in DB. Will use default lookback.");
            return null;
        }

        if (
            DateTime.TryParse(
                state.Value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var watermark
            )
        )
        {
            _logger.LogInformation("Watermark loaded from DB: {Watermark}", watermark);
            return watermark;
        }

        return null;
    }

    public async Task UpdateWatermarkAsync(DateTime processedAt)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var state = await db.WorkerStates.FirstOrDefaultAsync(w => w.Key == WatermarkKey);

        if (state == null)
        {
            db.WorkerStates.Add(
                new WorkerState { Key = WatermarkKey, Value = processedAt.ToString("O") }
            );
        }
        else
        {
            state.Value = processedAt.ToString("O");
        }

        await db.SaveChangesAsync();

        _logger.LogInformation("Watermark updated in DB to {Watermark}", processedAt);
    }
}
