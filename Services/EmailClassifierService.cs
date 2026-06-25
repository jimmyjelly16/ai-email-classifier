using System.Text.Json;
using EmailClassifier.Llm;
using EmailClassifier.Models;

namespace EmailClassifier.Services;

public sealed class EmailClassifierService : IEmailClassifierService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IChatProviderFactory _factory;
    private readonly ILogger<EmailClassifierService> _logger;

    public EmailClassifierService(
        IChatProviderFactory factory,
        ILogger<EmailClassifierService> logger
    )
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<EmailClassificationResult> ClassifyAsync(
        string subject,
        string body,
        CancellationToken cancellationToken = default
    )
    {
        var provider = _factory.GetActive();

        var systemPrompt = """
            You are an email classification assistant for a manufacturing company's
            customer service inbox. Classify the email and respond ONLY with a JSON
            object, no extra text, in this exact format:
            {
              "category": "Sales Inquiry" | "Technical Support" | "Billing" | "Other",
              "priority": "High" | "Medium" | "Low",
              "summary": "one sentence summary of the email",
              "assignedTo": "Sales" | "Engineering" | "Accounting" | "General Support"
            }
            """;

        var request = new ChatCompletionRequest
        {
            ForceJsonOutput = true,
            Temperature = 0.0,
            Messages = new[]
            {
                new Llm.ChatMessage(ChatRole.System, systemPrompt),
                new Llm.ChatMessage(ChatRole.User, $"Subject: {subject}\n\nBody: {body}"),
            },
        };

        var result = await provider.CompleteAsync(request, cancellationToken);

        _logger.LogInformation(
            "Classification done. Provider={Provider} Model={Model} In={In} Out={Out}",
            result.ProviderName,
            result.ModelUsed,
            result.InputTokens,
            result.OutputTokens
        );

        return JsonSerializer.Deserialize<EmailClassificationResult>(result.Content, JsonOpts)
            ?? throw new InvalidOperationException("Failed to deserialize LLM response.");
    }
}
