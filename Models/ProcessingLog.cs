namespace EmailClassifier.Models;

public class ProcessingLog
{
    public int Id { get; set; }
    public DateTime RunAt { get; set; }
    public int ProcessedCount { get; set; }
    public int FailedCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
