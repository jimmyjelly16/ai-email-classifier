using Microsoft.Extensions.Options;

namespace EmailClassifier.Llm;

public interface IChatProviderFactory
{
    IChatCompletionProvider GetActive();
    IChatCompletionProvider Get(string name);
}

public sealed class ChatProviderFactory : IChatProviderFactory
{
    private readonly IReadOnlyDictionary<string, IChatCompletionProvider> _providers;
    private readonly LlmOptions _options;

    public ChatProviderFactory(
        IEnumerable<IChatCompletionProvider> providers,
        IOptions<LlmOptions> options
    )
    {
        _providers = providers.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        _options = options.Value;
    }

    public IChatCompletionProvider GetActive() => Get(_options.Provider);

    public IChatCompletionProvider Get(string name) =>
        _providers.TryGetValue(name, out var p)
            ? p
            : throw new InvalidOperationException($"Unregistered LLM provider: {name}");
}
