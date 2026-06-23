using EmailClassifier.Data;
using EmailClassifier.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EmailClassifier.Services;

public class EmailProcessingService
{
    private readonly AppDbContext _db;
    private readonly IEmailClassifierService _classifier;
    private readonly WatermarkService _watermark;
    private readonly WorkerOptions _options;
    private readonly ILogger<EmailProcessingService> _logger;

    public EmailProcessingService(
        AppDbContext db,
        IEmailClassifierService classifier,
        WatermarkService watermark,
        IOptions<WorkerOptions> options,
        ILogger<EmailProcessingService> logger
    )
    {
        _db = db;
        _classifier = classifier;
        _watermark = watermark;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ProcessPendingEmailsAsync(CancellationToken cancellationToken)
    {
        var runAt = DateTime.UtcNow;
        var processedCount = 0;
        var failedCount = 0;
        string? errorMessage = null;

        var since =
            await _watermark.GetWatermarkAsync()
            ?? DateTime.UtcNow.AddMinutes(-_options.WatermarkMinutesBack);

        _logger.LogInformation("Processing emails since {Since}", since);

        var pendingEmails = await _db
            .EmailInboxes.Where(e => e.Status == EmailStatus.Pending && e.CreatedAt >= since)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Found {Count} pending emails since {Since}.",
            pendingEmails.Count,
            since
        );

        if (pendingEmails.Count == 0)
        {
            _logger.LogInformation("No pending emails to process.");
            await WriteProcessingLogAsync(runAt, 0, 0, null);
            return;
        }

        foreach (var email in pendingEmails)
        {
            try
            {
                email.Status = EmailStatus.Processing;
                await _db.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Classifying email Id={Id} Subject={Subject}",
                    email.Id,
                    email.Subject
                );

                var result = await _classifier.ClassifyAsync(
                    email.Subject,
                    email.Body,
                    cancellationToken
                );

                email.Category = result.Category;
                email.Priority = result.Priority;
                email.AiSummary = result.Summary;
                email.AssignedTo = result.AssignedTo;
                email.Status = EmailStatus.Completed;
                email.ProcessedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync(cancellationToken);

                processedCount++;

                _logger.LogInformation(
                    "Email Id={Id} classified. Category={Category} Priority={Priority} AssignedTo={AssignedTo}",
                    email.Id,
                    result.Category,
                    result.Priority,
                    result.AssignedTo
                );
            }
            catch (Exception ex)
            {
                failedCount++;
                errorMessage = ex.Message;

                _logger.LogError(
                    ex,
                    "Failed to classify email Id={Id} Subject={Subject}",
                    email.Id,
                    email.Subject
                );

                email.Status = EmailStatus.Failed;
                email.RetryCount += 1;

                try
                {
                    await _db.SaveChangesAsync(cancellationToken);
                }
                catch (Exception saveEx)
                {
                    _logger.LogError(
                        saveEx,
                        "Failed to save Failed status for email Id={Id}",
                        email.Id
                    );
                }
            }
        }

        await _watermark.UpdateWatermarkAsync(runAt);

        await WriteProcessingLogAsync(runAt, processedCount, failedCount, errorMessage);

        _logger.LogInformation(
            "Processing completed. Processed={Processed} Failed={Failed}",
            processedCount,
            failedCount
        );
    }

    private async Task WriteProcessingLogAsync(
        DateTime runAt,
        int processedCount,
        int failedCount,
        string? errorMessage
    )
    {
        var log = new ProcessingLog
        {
            RunAt = runAt,
            ProcessedCount = processedCount,
            FailedCount = failedCount,
            ErrorMessage = errorMessage,
        };

        _db.ProcessingLogs.Add(log);
        await _db.SaveChangesAsync();
    }
}
