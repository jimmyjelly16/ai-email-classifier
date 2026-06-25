namespace EmailClassifier.Llm;

public enum ChatRole
{
    System,
    User,
    Assistant,
}

public record ChatMessage(ChatRole Role, string Content);

public record ChatCompletionRequest
{
    public required IReadOnlyList<ChatMessage> Messages { get; init; }
    public string? Model { get; init; }
    public int MaxTokens { get; init; } = 1024;
    public double Temperature { get; init; } = 0.0;
    public bool ForceJsonOutput { get; init; } = false;
}

public record ChatCompletionResult
{
    public required string Content { get; init; }
    public int? InputTokens { get; init; }
    public int? OutputTokens { get; init; }
    public string? FinishReason { get; init; }
    public required string ProviderName { get; init; }
    public required string ModelUsed { get; init; }
}

public interface IChatCompletionProvider
{
    string Name { get; }

    Task<ChatCompletionResult> CompleteAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default
    );
}
