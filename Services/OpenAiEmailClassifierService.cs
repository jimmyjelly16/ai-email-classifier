using System.Text.Json;
using EmailClassifier.Models;
using OpenAI.Chat;

namespace EmailClassifier.Services;

public class OpenAiEmailClassifierService : IEmailClassifierService
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<OpenAiEmailClassifierService> _logger;

    public OpenAiEmailClassifierService(ILogger<OpenAiEmailClassifierService> logger)
    {
        _logger = logger;

        var apiKey =
            Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? throw new InvalidOperationException(
                "OPENAI_API_KEY environment variable is not set."
            );

        _chatClient = new ChatClient("gpt-4o-mini", apiKey);
    }

    public async Task<EmailClassificationResult> ClassifyAsync(
        string subject,
        string body,
        CancellationToken cancellationToken = default
    )
    {
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

        var userPrompt = $"Subject: {subject}\n\nBody: {body}";

        var messages = new ChatMessage[]
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt),
        };

        var options = new ChatCompletionOptions
        {
            Temperature = 0.0f,
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
        };

        _logger.LogInformation(
            "Calling OpenAI for email classification. Subject={Subject}",
            subject
        );

        ChatCompletion completion = await _chatClient.CompleteChatAsync(
            messages,
            options,
            cancellationToken
        );

        var rawJson = completion.Content[0].Text;

        _logger.LogInformation("OpenAI raw response: {RawJson}", rawJson);

        var result =
            JsonSerializer.Deserialize<EmailClassificationResult>(
                rawJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? throw new InvalidOperationException("Failed to deserialize OpenAI response.");

        return result;
    }
}
