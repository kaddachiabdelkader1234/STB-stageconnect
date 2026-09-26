using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartek.Common.Errors;
using Smartek.Common.Security;
using SubjectMatching.Service.Data;
using SubjectMatching.Service.DTOs;
using SubjectMatching.Service.Models;
using SubjectMatching.Service.Services;

namespace SubjectMatching.Service.Controllers;

[ApiController]
[Route("api/v1/matching")]
[Produces("application/json")]
[Authorize]
public class MatchingController : ControllerBase
{
    private readonly SubjectMatchingDbContext _dbContext;
    private readonly IEnumerable<IAiMatchingProvider> _providers;
    private readonly MatchingEngine _matchingEngine;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MatchingController> _logger;

    public MatchingController(
        SubjectMatchingDbContext dbContext,
        IEnumerable<IAiMatchingProvider> providers,
        MatchingEngine matchingEngine,
        IConfiguration configuration,
        ILogger<MatchingController> logger)
    {
        _dbContext = dbContext;
        _providers = providers;
        _matchingEngine = matchingEngine;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Analyzes a candidate's CV using the configured AI matching provider (SharpAPI or fallback Mock).
    /// Results are cached in candidate_profiles to avoid duplicate external API charges.
    /// </summary>
    [HttpPost("candidates/{stagiaireId:guid}/analyze")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(CandidateProfileReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidateProfileReadDto>> AnalyzeCandidate(
        Guid stagiaireId,
        [FromQuery] bool force = false,
        [FromQuery] long? utilisateurId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing candidate CV for StagiaireId: {StagiaireId}, Force: {Force}", stagiaireId, force);

        // 1. Check existing cached profile unless force reload is requested
        var existingProfile = await _dbContext.CandidateProfiles
            .FirstOrDefaultAsync(p => p.StagiaireId == stagiaireId, cancellationToken);

        if (existingProfile != null && !force)
        {
            _logger.LogInformation("Returning cached candidate profile for {StagiaireId}", stagiaireId);
            return Ok(ToProfileDto(existingProfile));
        }

        // 2. Locate CV file on disk or storage path
        var cvStreamAndName = await ResolveCandidateCvStream(stagiaireId, cancellationToken);

        // 3. Select matching provider
        var preferredProviderName = (_configuration["AI_MATCHING_PROVIDER"] ?? "sharpapi").ToLowerInvariant();
        var provider = _providers.FirstOrDefault(p => p.ProviderName.Equals(preferredProviderName, StringComparison.OrdinalIgnoreCase))
                       ?? _providers.FirstOrDefault(p => p.ProviderName.Equals("mock", StringComparison.OrdinalIgnoreCase))
                       ?? _providers.First();

        ParsedCvData parsedData;
        string actualProviderUsed = provider.ProviderName;

        try
        {
            _logger.LogInformation("Invoking AI provider {Provider} for {StagiaireId}", provider.ProviderName, stagiaireId);
            parsedData = await provider.ParseResumeAsync(cvStreamAndName.Stream, cvStreamAndName.FileName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Preferred AI provider {Provider} failed. Degrading gracefully to MockMatchingProvider.", preferredProviderName);
            var mockProvider = _providers.FirstOrDefault(p => p.ProviderName == "mock") ?? new MockMatchingProvider(
                LoggerFactory.Create(b => b.AddConsole()).CreateLogger<MockMatchingProvider>());

            cvStreamAndName.Stream.Position = 0;
            parsedData = await mockProvider.ParseResumeAsync(cvStreamAndName.Stream, cvStreamAndName.FileName, cancellationToken);
            actualProviderUsed = "mock (fallback)";
        }
        finally
        {
            await cvStreamAndName.Stream.DisposeAsync();
        }

        // 4. Save or update candidate profile
        if (existingProfile == null)
        {
            existingProfile = new CandidateProfile
            {
                Id = Guid.NewGuid(),
                StagiaireId = stagiaireId,
                UtilisateurId = utilisateurId ?? 0,
                CvFileHash = cvStreamAndName.Hash,
                Education = parsedData.Education,
                Skills = parsedData.Skills,
                Experience = parsedData.Experience,
                Projects = parsedData.Projects,
                Languages = parsedData.Languages,
                ParsedAt = DateTime.UtcNow,
                Provider = actualProviderUsed
            };
            _dbContext.CandidateProfiles.Add(existingProfile);
        }
        else
        {
            existingProfile.CvFileHash = cvStreamAndName.Hash;
            existingProfile.Education = parsedData.Education;
            existingProfile.Skills = parsedData.Skills;
            existingProfile.Experience = parsedData.Experience;
            existingProfile.Projects = parsedData.Projects;
            existingProfile.Languages = parsedData.Languages;
            existingProfile.ParsedAt = DateTime.UtcNow;
            existingProfile.Provider = actualProviderUsed;
            if (utilisateurId.HasValue && utilisateurId.Value > 0)
            {
                existingProfile.UtilisateurId = utilisateurId.Value;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // 5. Precompute and refresh matches for all open subjects
        await RefreshCandidateMatches(existingProfile, cancellationToken);

        return Ok(ToProfileDto(existingProfile));
    }

    /// <summary>
    /// Gets recommendations for a candidate based on AI parsed skills and deterministic matching engine.
    /// </summary>
    [HttpGet("candidates/{stagiaireId:guid}/recommendations")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(List<SubjectMatchReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<SubjectMatchReadDto>>> GetRecommendations(
        Guid stagiaireId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _dbContext.CandidateProfiles
            .FirstOrDefaultAsync(p => p.StagiaireId == stagiaireId, cancellationToken);

        if (profile is null)
        {
            throw new NotFoundException("Le profil du candidat n'a pas encore été analysé. Veuillez d'abord déclencher l'analyse IA.");
        }

        // Fetch or recalculate matches with current open subjects
        var matches = await _dbContext.SubjectMatches
            .Include(m => m.Subject)
            .Where(m => m.StagiaireId == stagiaireId && m.Subject != null && m.Subject.Status == SubjectStatus.Open)
            .OrderByDescending(m => m.CompatibilityScore)
            .ToListAsync(cancellationToken);

        if (matches.Count == 0)
        {
            await RefreshCandidateMatches(profile, cancellationToken);

            matches = await _dbContext.SubjectMatches
                .Include(m => m.Subject)
                .Where(m => m.StagiaireId == stagiaireId && m.Subject != null && m.Subject.Status == SubjectStatus.Open)
                .OrderByDescending(m => m.CompatibilityScore)
                .ToListAsync(cancellationToken);
        }

        var results = matches.Select(m => new SubjectMatchReadDto
        {
            Id = m.Id,
            StagiaireId = m.StagiaireId,
            SubjectId = m.SubjectId,
            SubjectTitle = m.Subject?.Title ?? "Sujet",
            Department = m.Subject?.Department ?? "",
            TypeStage = m.Subject?.TypeStage ?? "PFE",
            Difficulty = m.Subject?.Difficulty ?? SubjectDifficulty.Intermediate,
            CompatibilityScore = m.CompatibilityScore,
            MatchedSkills = m.MatchedSkills,
            MissingSkills = m.MissingSkills,
            Explanation = m.Explanation,
            AvailablePositions = m.Subject?.AvailablePositions ?? 0,
            FilledPositions = m.Subject?.FilledPositions ?? 0,
            Status = m.Subject?.Status ?? SubjectStatus.Open,
            GeneratedAt = m.GeneratedAt
        }).ToList();

        return Ok(results);
    }

    /// <summary>
    /// Gets the cached candidate profile if available.
    /// </summary>
    [HttpGet("candidates/{stagiaireId:guid}/profile")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(CandidateProfileReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidateProfileReadDto>> GetCandidateProfile(
        Guid stagiaireId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _dbContext.CandidateProfiles
            .FirstOrDefaultAsync(p => p.StagiaireId == stagiaireId, cancellationToken);

        if (profile is null)
        {
            throw NotFoundException.For("Le profil IA du candidat", stagiaireId);
        }

        return Ok(ToProfileDto(profile));
    }

    private async Task RefreshCandidateMatches(CandidateProfile profile, CancellationToken cancellationToken)
    {
        var openSubjects = await _dbContext.Subjects
            .Where(s => s.Status == SubjectStatus.Open)
            .ToListAsync(cancellationToken);

        var existingMatches = await _dbContext.SubjectMatches
            .Where(m => m.StagiaireId == profile.StagiaireId)
            .ToListAsync(cancellationToken);

        _dbContext.SubjectMatches.RemoveRange(existingMatches);

        foreach (var subject in openSubjects)
        {
            var result = _matchingEngine.CalculateMatch(profile, subject);
            var match = new SubjectMatch
            {
                Id = Guid.NewGuid(),
                StagiaireId = profile.StagiaireId,
                SubjectId = subject.Id,
                CompatibilityScore = result.CompatibilityScore,
                MatchedSkills = result.MatchedSkills,
                MissingSkills = result.MissingSkills,
                Explanation = result.Explanation,
                GeneratedAt = DateTime.UtcNow
            };
            _dbContext.SubjectMatches.Add(match);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<(Stream Stream, string FileName, string Hash)> ResolveCandidateCvStream(
        Guid stagiaireId,
        CancellationToken cancellationToken)
    {
        // Try searching standard storage locations
        var searchRoots = new[]
        {
            "/app/storage/stagiaires",
            "/app/storage",
            "C:\\Users\\kayder\\OneDrive\\Desktop\\gestion-des-stagiere-gestion-des-stagiere\\gestion-des-stagiere-gestion-des-stagiere\\storage",
            Path.Combine(AppContext.BaseDirectory, "storage")
        };

        foreach (var root in searchRoots)
        {
            if (!Directory.Exists(root)) continue;

            // Search files matching stagiaireId in filename or subdirectory
            var files = Directory.GetFiles(root, $"*{stagiaireId}*", SearchOption.AllDirectories);
            if (files.Length > 0)
            {
                var filePath = files[0];
                var bytes = await System.IO.File.ReadAllBytesAsync(filePath, cancellationToken);
                var hash = Convert.ToHexString(SHA256.HashData(bytes));
                return (new MemoryStream(bytes), Path.GetFileName(filePath), hash);
            }

            // Also check under cv/ subdirectory
            var cvDir = Path.Combine(root, "cv");
            if (Directory.Exists(cvDir))
            {
                var cvFiles = Directory.GetFiles(cvDir, "*.pdf");
                if (cvFiles.Length > 0)
                {
                    var filePath = cvFiles[0];
                    var bytes = await System.IO.File.ReadAllBytesAsync(filePath, cancellationToken);
                    var hash = Convert.ToHexString(SHA256.HashData(bytes));
                    return (new MemoryStream(bytes), Path.GetFileName(filePath), hash);
                }
            }
        }

        // If no file exists on physical disk (e.g. testing or mock data without uploaded PDF),
        // provide a synthetic placeholder stream so parsing can proceed deterministically
        var dummyBytes = System.Text.Encoding.UTF8.GetBytes($"CV Candidat Stagiaire {stagiaireId}");
        var dummyHash = Convert.ToHexString(SHA256.HashData(dummyBytes));
        return (new MemoryStream(dummyBytes), $"cv-{stagiaireId}.pdf", dummyHash);
    }

    private static CandidateProfileReadDto ToProfileDto(CandidateProfile p) => new()
    {
        Id = p.Id,
        StagiaireId = p.StagiaireId,
        UtilisateurId = p.UtilisateurId,
        Education = p.Education,
        Skills = p.Skills,
        Experience = p.Experience,
        Projects = p.Projects,
        Languages = p.Languages,
        ParsedAt = p.ParsedAt,
        Provider = p.Provider
    };
}
