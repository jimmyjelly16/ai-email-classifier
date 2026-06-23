using EmailClassifier.Models;

namespace EmailClassifier.Services;

public interface IEmailClassifierService
{
    Task<EmailClassificationResult> ClassifyAsync(
        string subject,
        string body,
        CancellationToken cancellationToken = default
    );
}
