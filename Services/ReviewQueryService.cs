using EmailClassifier.Data;
using EmailClassifier.Models;
using Microsoft.EntityFrameworkCore;

namespace EmailClassifier.Services;

public class ReviewQueryService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ReviewQueryService> _logger;

    public ReviewQueryService(AppDbContext db, ILogger<ReviewQueryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Returns human-confirmed correct examples for future Few-Shot prompt material.
    /// </summary>
    public async Task<List<EmailInbox>> GetConfirmedCorrectExamplesAsync(
        int limit = 5,
        CancellationToken ct = default
    )
    {
        return await _db
            .EmailInboxes.Where(e => e.IsReviewed && e.IsCorrect == true && e.Category != null)
            .OrderByDescending(e => e.ReviewedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Returns incorrectly classified emails flagged by human review, for prompt analysis.
    /// </summary>
    public async Task<List<EmailInbox>> GetIncorrectlyClassifiedAsync(
        CancellationToken ct = default
    )
    {
        return await _db
            .EmailInboxes.Where(e => e.IsReviewed && e.IsCorrect == false)
            .OrderByDescending(e => e.ReviewedAt)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Returns failed emails that have exceeded the retry limit and need human attention.
    /// </summary>
    public async Task<List<EmailInbox>> GetFailedNeedingAttentionAsync(
        int maxRetry = 3,
        CancellationToken ct = default
    )
    {
        return await _db
            .EmailInboxes.Where(e => e.Status == EmailStatus.Failed && e.RetryCount >= maxRetry)
            .OrderByDescending(e => e.UpdatedAt)
            .ToListAsync(ct);
    }
}
