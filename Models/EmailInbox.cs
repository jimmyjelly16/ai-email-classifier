namespace EmailClassifier.Models;

public class EmailInbox
{
    public int Id { get; set; }
    public string SenderEmail { get; set; } = string.Empty;
    public int? ContactId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public EmailStatus Status { get; set; } = EmailStatus.Pending;
    public string? Category { get; set; }
    public string? Priority { get; set; }
    public string? AiSummary { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; } = 0;
    public bool IsReviewed { get; set; }
    public bool? IsCorrect { get; set; }
    public string? CorrectedCategory { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Contact? Contact { get; set; }
}
