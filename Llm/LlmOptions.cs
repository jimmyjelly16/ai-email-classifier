namespace EmailClassifier.Llm;

public sealed class LlmOptions
{
    public string Provider { get; set; } = "OpenAI";
}

public sealed class OpenAiOptions
{
    public string ApiKey { get; set; } = "";
    public string DefaultModel { get; set; } = "gpt-4o-mini";
}
