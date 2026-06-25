namespace EmailClassifier;

public static partial class LogMessages
{
    // Worker
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Email Classifier Worker started. Interval={Interval}s"
    )]
    public static partial void WorkerStarted(ILogger logger, int interval);

    [LoggerMessage(Level = LogLevel.Information, Message = "Worker tick at {Time}")]
    public static partial void WorkerTick(ILogger logger, DateTimeOffset time);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled error in Worker tick.")]
    public static partial void WorkerTickError(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Worker sleeping for {Interval}s")]
    public static partial void WorkerSleeping(ILogger logger, int interval);

    [LoggerMessage(Level = LogLevel.Information, Message = "Email Classifier Worker stopped.")]
    public static partial void WorkerStopped(ILogger logger);

    // EmailProcessingService
    [LoggerMessage(Level = LogLevel.Information, Message = "Processing emails since {Since}")]
    public static partial void ProcessingSince(ILogger logger, DateTime since);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Found {Count} emails to process (Pending since {Since} + retryable Failed)."
    )]
    public static partial void FoundEmailsToProcess(ILogger logger, int count, DateTime since);

    [LoggerMessage(Level = LogLevel.Information, Message = "No pending emails to process.")]
    public static partial void NoPendingEmails(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Classifying email Id={Id} Subject={Subject}"
    )]
    public static partial void ClassifyingEmail(ILogger logger, int id, string subject);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Email Id={Id} classified. Category={Category} Priority={Priority} AssignedTo={AssignedTo}"
    )]
    public static partial void EmailClassified(
        ILogger logger,
        int id,
        string category,
        string priority,
        string assignedTo
    );

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to classify email Id={Id} Subject={Subject}"
    )]
    public static partial void ClassifyFailed(ILogger logger, Exception ex, int id, string subject);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to save Failed status for email Id={Id}"
    )]
    public static partial void SaveFailedStatusError(ILogger logger, Exception ex, int id);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Processing completed. Processed={Processed} Failed={Failed}"
    )]
    public static partial void ProcessingCompleted(ILogger logger, int processed, int failed);

    // WatermarkService
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "No watermark found in DB. Will use default lookback."
    )]
    public static partial void NoWatermarkFound(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Watermark loaded from DB: {Watermark}")]
    public static partial void WatermarkLoaded(ILogger logger, DateTime watermark);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Watermark updated in DB to {Watermark}"
    )]
    public static partial void WatermarkUpdated(ILogger logger, DateTime watermark);

    // EmailClassifierService
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Classification done. Provider={Provider} Model={Model} In={In} Out={Out}"
    )]
    public static partial void ClassificationDone(
        ILogger logger,
        string provider,
        string model,
        int? @in,
        int? @out
    );

    // OpenAiChatProvider
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Calling OpenAI provider. Model={Model} ForceJson={ForceJson}"
    )]
    public static partial void CallingOpenAi(ILogger logger, string model, bool forceJson);
}
