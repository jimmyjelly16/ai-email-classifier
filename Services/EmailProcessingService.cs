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

        LogMessages.ProcessingSince(_logger, since);

        var maxRetry = _options.MaxRetryCount;

        var emailsToProcess = await _db
            .EmailInboxes.Where(e =>
                (e.Status == EmailStatus.Pending && e.CreatedAt >= since)
                || (e.Status == EmailStatus.Failed && e.RetryCount < maxRetry)
            )
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        LogMessages.FoundEmailsToProcess(_logger, emailsToProcess.Count, since);

        if (emailsToProcess.Count == 0)
        {
            LogMessages.NoPendingEmails(_logger);
            await WriteProcessingLogAsync(runAt, 0, 0, null);
            return;
        }

        foreach (var email in emailsToProcess)
        {
            try
            {
                email.Status = EmailStatus.Processing;
                await _db.SaveChangesAsync(cancellationToken);

                LogMessages.ClassifyingEmail(_logger, email.Id, email.Subject);

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

                LogMessages.EmailClassified(
                    _logger,
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

                LogMessages.ClassifyFailed(_logger, ex, email.Id, email.Subject);

                email.Status = EmailStatus.Failed;
                email.RetryCount += 1;

                try
                {
                    await _db.SaveChangesAsync(cancellationToken);
                }
                catch (Exception saveEx)
                {
                    LogMessages.SaveFailedStatusError(_logger, saveEx, email.Id);
                }
            }
        }

        await _watermark.UpdateWatermarkAsync(runAt);

        await WriteProcessingLogAsync(runAt, processedCount, failedCount, errorMessage);

        LogMessages.ProcessingCompleted(_logger, processedCount, failedCount);
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
