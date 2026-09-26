namespace SubjectMatching.Service.Services;

public class ParsedCvData
{
    public string? CandidateName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Education { get; set; }
    public List<string> Skills { get; set; } = new();
    public string? Experience { get; set; }
    public List<string> Projects { get; set; } = new();
    public List<string> Languages { get; set; } = new();
}

public interface IAiMatchingProvider
{
    string ProviderName { get; }
    Task<ParsedCvData> ParseResumeAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
}
