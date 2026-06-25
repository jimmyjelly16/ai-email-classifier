namespace EmailClassifier.Models;

public class WorkerOptions
{
    public int IntervalSeconds { get; set; } = 30;
    public int WatermarkMinutesBack { get; set; } = 1440;
    public int MaxRetryCount { get; set; } = 3;
}
