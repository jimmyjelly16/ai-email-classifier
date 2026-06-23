namespace EmailClassifier.Models;

public class EmailClassificationResult
{
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string AssignedTo { get; set; } = string.Empty;
}
