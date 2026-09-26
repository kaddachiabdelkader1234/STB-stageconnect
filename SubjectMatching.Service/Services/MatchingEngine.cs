using SubjectMatching.Service.Models;

namespace SubjectMatching.Service.Services;

public class MatchCalculationResult
{
    public decimal CompatibilityScore { get; set; }
    public List<string> MatchedSkills { get; set; } = new();
    public List<string> MissingSkills { get; set; } = new();
    public string Explanation { get; set; } = string.Empty;
}

public class MatchingEngine
{
    public MatchCalculationResult CalculateMatch(CandidateProfile candidate, InternshipSubject subject)
    {
        var candidateSkills = candidate.Skills ?? new List<string>();
        var requiredSkills = subject.RequiredSkills ?? new List<string>();
        var preferredSkills = subject.PreferredSkills ?? new List<string>();

        var matchedSkills = new List<string>();
        var missingSkills = new List<string>();

        // 1. Required Skills (50% weight)
        decimal requiredScore = 0;
        int matchedRequiredCount = 0;

        foreach (var req in requiredSkills)
        {
            if (HasSkill(candidate, req))
            {
                matchedSkills.Add(req);
                matchedRequiredCount++;
            }
            else
            {
                missingSkills.Add(req);
            }
        }

        if (requiredSkills.Count > 0)
        {
            requiredScore = ((decimal)matchedRequiredCount / requiredSkills.Count) * 50m;
        }
        else
        {
            requiredScore = 50m; // No specific requirement means candidate meets criteria
        }

        // 2. Preferred Skills (20% weight)
        decimal preferredScore = 0;
        int matchedPreferredCount = 0;

        foreach (var pref in preferredSkills)
        {
            if (HasSkill(candidate, pref))
            {
                if (!matchedSkills.Contains(pref, StringComparer.OrdinalIgnoreCase))
                {
                    matchedSkills.Add(pref);
                }
                matchedPreferredCount++;
            }
            else
            {
                if (!missingSkills.Contains(pref, StringComparer.OrdinalIgnoreCase))
                {
                    missingSkills.Add(pref);
                }
            }
        }

        if (preferredSkills.Count > 0)
        {
            preferredScore = ((decimal)matchedPreferredCount / preferredSkills.Count) * 20m;
        }
        else
        {
            preferredScore = 20m;
        }

        // 3. Education Match (15% weight)
        decimal educationScore = 10m; // base score for university candidate
        var candidateEdu = candidate.Education?.ToLowerInvariant() ?? "";
        var reqEdu = subject.EducationRequirements?.ToLowerInvariant() ?? "";

        if (string.IsNullOrWhiteSpace(reqEdu))
        {
            educationScore = 15m;
        }
        else
        {
            if (candidateEdu.Contains("ingénieur") || candidateEdu.Contains("master") || candidateEdu.Contains("bac+5"))
            {
                educationScore = 15m;
            }
            else if (candidateEdu.Contains("licence") || candidateEdu.Contains("bac+3"))
            {
                educationScore = subject.Difficulty == SubjectDifficulty.Advanced ? 10m : 15m;
            }
            else if (!string.IsNullOrWhiteSpace(candidateEdu))
            {
                educationScore = 12m;
            }
        }

        // 4. Experience & Projects Match (15% weight)
        decimal experienceScore = 5m;
        var projects = candidate.Projects ?? new List<string>();
        var experience = candidate.Experience ?? "";

        if (projects.Count > 0)
        {
            experienceScore += Math.Min(6m, projects.Count * 2m);
        }

        if (!string.IsNullOrWhiteSpace(experience) && experience.Length > 20)
        {
            experienceScore += 4m;
        }

        experienceScore = Math.Min(15m, experienceScore);

        // Total Compatibility Score
        var totalScore = Math.Round(requiredScore + preferredScore + educationScore + experienceScore, 2);
        totalScore = Math.Clamp(totalScore, 0m, 100m);

        // Build Explanation
        var explanationParts = new List<string>();

        if (requiredSkills.Count > 0)
        {
            explanationParts.Add($"Le candidat maîtrise {matchedRequiredCount}/{requiredSkills.Count} compétence(s) requise(s)");
        }
        else
        {
            explanationParts.Add("Aucun prérequis technique strict pour ce sujet");
        }

        if (preferredSkills.Count > 0)
        {
            explanationParts.Add($"{matchedPreferredCount}/{preferredSkills.Count} compétence(s) souhaitée(s) validée(s)");
        }

        if (matchedSkills.Count > 0)
        {
            var topSkills = string.Join(", ", matchedSkills.Take(4));
            explanationParts.Add($"Atouts identifiés : {topSkills}");
        }

        if (missingSkills.Count > 0)
        {
            var topMissing = string.Join(", ", missingSkills.Take(3));
            explanationParts.Add($"Compétences à consolider : {topMissing}");
        }

        if (!string.IsNullOrWhiteSpace(candidate.Education))
        {
            explanationParts.Add("Formation académique compatible avec les objectifs du stage.");
        }

        var explanation = string.Join(". ", explanationParts) + ".";

        return new MatchCalculationResult
        {
            CompatibilityScore = totalScore,
            MatchedSkills = matchedSkills,
            MissingSkills = missingSkills,
            Explanation = explanation
        };
    }

    private static bool HasSkill(CandidateProfile candidate, string skill)
    {
        if (string.IsNullOrWhiteSpace(skill)) return false;

        var target = Normalize(skill);

        if (candidate.Skills != null)
        {
            foreach (var s in candidate.Skills)
            {
                if (Normalize(s) == target ||
                    Normalize(s).Contains(target) ||
                    target.Contains(Normalize(s)))
                {
                    return true;
                }
            }
        }

        // Also search in candidate projects & experience
        var fullText = (candidate.Experience + " " + string.Join(" ", candidate.Projects ?? new List<string>())).ToLowerInvariant();
        return fullText.Contains(target);
    }

    private static string Normalize(string str)
    {
        return str.Trim().ToLowerInvariant()
            .Replace("-", "")
            .Replace(" ", "")
            .Replace(".", "");
    }
}
