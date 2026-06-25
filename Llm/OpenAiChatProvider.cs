using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace EmailClassifier.Llm;

public sealed class OpenAiChatProvider : IChatCompletionProvider
{
    public string Name => "OpenAI";

    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiChatProvider> _logger;

    public OpenAiChatProvider(IOptions<OpenAiOptions> options, ILogger<OpenAiChatProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ChatCompletionResult> CompleteAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var model = request.Model ?? _options.DefaultModel;

        var client = new ChatClient(model, _options.ApiKey);

        var messages = request
            .Messages.Select<ChatMessage, OpenAI.Chat.ChatMessage>(m =>
                m.Role switch
                {
                    ChatRole.System => new SystemChatMessage(m.Content),
                    ChatRole.User => new UserChatMessage(m.Content),
                    ChatRole.Assistant => new AssistantChatMessage(m.Content),
                    _ => throw new ArgumentOutOfRangeException(nameof(m.Role)),
                }
            )
            .ToList();

        var options = new ChatCompletionOptions { Temperature = (float)request.Temperature };
        if (request.ForceJsonOutput)
            options.ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat();

        _logger.LogInformation(
            "Calling OpenAI provider. Model={Model} ForceJson={ForceJson}",
            model,
            request.ForceJsonOutput
        );

        ChatCompletion completion = await client.CompleteChatAsync(
            messages,
            options,
            cancellationToken
        );

        var content = completion.Content[0].Text;

        return new ChatCompletionResult
        {
            Content = content,
            InputTokens = completion.Usage?.InputTokenCount,
            OutputTokens = completion.Usage?.OutputTokenCount,
            FinishReason = completion.FinishReason.ToString(),
            ProviderName = Name,
            ModelUsed = model,
        };
    }
}
