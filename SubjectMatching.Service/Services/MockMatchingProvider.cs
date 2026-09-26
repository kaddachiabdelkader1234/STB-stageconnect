using Microsoft.Extensions.Logging;

namespace SubjectMatching.Service.Services;

public class MockMatchingProvider : IAiMatchingProvider
{
    private readonly ILogger<MockMatchingProvider> _logger;

    public string ProviderName => "mock";

    public MockMatchingProvider(ILogger<MockMatchingProvider> logger)
    {
        _logger = logger;
    }

    public Task<ParsedCvData> ParseResumeAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Parsing CV with MockMatchingProvider for {FileName}", fileName);

        // Deterministic set of skills representing common STB candidate profiles
        var data = new ParsedCvData
        {
            CandidateName = "Candidat STB",
            Email = "candidat@stb.tn",
            Phone = "+216 71 000 000",
            Education = "Diplôme National d'Ingénieur en Informatique / Génie Logiciel (Bac+5)",
            Skills = new List<string>
            {
                ".NET", "C#", "ASP.NET Core", "Angular", "TypeScript",
                "Spring Boot", "Java", "PostgreSQL", "Docker", "RabbitMQ",
                "REST API", "Git", "Clean Architecture", "Microservices"
            },
            Experience = "Projets académiques de fin d'études et stages d'été en développement Full-Stack web et architectures distribuées.",
            Projects = new List<string>
            {
                "Plateforme de gestion bancaire en Microservices (.NET / Angular)",
                "Application de traitement temps réel d'événements avec RabbitMQ",
                "Dashboard analytique avec graphiques de performance"
            },
            Languages = new List<string>
            {
                "Français (Courant)",
                "Anglais (Professionnel)",
                "Arabe (Maternel)"
            }
        };

        return Task.FromResult(data);
    }
}
