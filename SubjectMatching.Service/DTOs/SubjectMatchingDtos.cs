using System.ComponentModel.DataAnnotations;
using SubjectMatching.Service.Models;

namespace SubjectMatching.Service.DTOs;

public class SubjectCreateDto
{
    [Required]
    [MaxLength(250)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(4000)]
    public string ProblemStatement { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Department { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string TypeStage { get; set; } = "PFE";

    public List<string> RequiredSkills { get; set; } = new();

    public List<string> PreferredSkills { get; set; } = new();

    [MaxLength(500)]
    public string? EducationRequirements { get; set; }

    [MaxLength(500)]
    public string? ExperienceRequirements { get; set; }

    public SubjectDifficulty Difficulty { get; set; } = SubjectDifficulty.Intermediate;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    [Range(1, 100)]
    public int AvailablePositions { get; set; } = 1;
}

public class SubjectUpdateDto : SubjectCreateDto
{
    public SubjectStatus Status { get; set; } = SubjectStatus.Open;
}

public class SubjectReadDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ProblemStatement { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string TypeStage { get; set; } = "PFE";
    public List<string> RequiredSkills { get; set; } = new();
    public List<string> PreferredSkills { get; set; } = new();
    public string? EducationRequirements { get; set; }
    public string? ExperienceRequirements { get; set; }
    public SubjectDifficulty Difficulty { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int AvailablePositions { get; set; }
    public int FilledPositions { get; set; }
    public SubjectStatus Status { get; set; }
    public long CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CandidateProfileReadDto
{
    public Guid Id { get; set; }
    public Guid StagiaireId { get; set; }
    public long UtilisateurId { get; set; }
    public string? Education { get; set; }
    public List<string> Skills { get; set; } = new();
    public string? Experience { get; set; }
    public List<string> Projects { get; set; } = new();
    public List<string> Languages { get; set; } = new();
    public DateTime ParsedAt { get; set; }
    public string Provider { get; set; } = "mock";
}

public class SubjectMatchReadDto
{
    public Guid Id { get; set; }
    public Guid StagiaireId { get; set; }
    public Guid SubjectId { get; set; }
    public string SubjectTitle { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string TypeStage { get; set; } = string.Empty;
    public SubjectDifficulty Difficulty { get; set; }
    public decimal CompatibilityScore { get; set; }
    public List<string> MatchedSkills { get; set; } = new();
    public List<string> MissingSkills { get; set; } = new();
    public string Explanation { get; set; } = string.Empty;
    public int AvailablePositions { get; set; }
    public int FilledPositions { get; set; }
    public SubjectStatus Status { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class ProposeSubjectDto
{
    [Required]
    public Guid StagiaireId { get; set; }

    [Required]
    public Guid SubjectId { get; set; }

    public long UtilisateurId { get; set; }

    public string? CandidateEmail { get; set; }

    public string? CandidateName { get; set; }

    public string? EncadrantNom { get; set; }
}

public class SubjectAssignmentReadDto
{
    public Guid Id { get; set; }
    public Guid StagiaireId { get; set; }
    public long UtilisateurId { get; set; }
    public Guid SubjectId { get; set; }
    public string SubjectTitle { get; set; } = string.Empty;
    public string SubjectDescription { get; set; } = string.Empty;
    public string ProblemStatement { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public List<string> RequiredSkills { get; set; } = new();
    public AssignmentStatus Status { get; set; }
    public decimal ScoreAtProposal { get; set; }
    public DateTime ProposedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}

public class CreateChangeRequestDto
{
    [Required]
    public Guid AssignmentId { get; set; }

    [Required]
    public Guid RequestedSubjectId { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Reason { get; set; } = string.Empty;
}

public class ReviewChangeRequestDto
{
    public bool Approved { get; set; }

    [MaxLength(2000)]
    public string? Comment { get; set; }
}

public class SubjectChangeRequestReadDto
{
    public Guid Id { get; set; }
    public Guid AssignmentId { get; set; }
    public Guid StagiaireId { get; set; }
    public long UtilisateurId { get; set; }
    public Guid CurrentSubjectId { get; set; }
    public string CurrentSubjectTitle { get; set; } = string.Empty;
    public Guid RequestedSubjectId { get; set; }
    public string RequestedSubjectTitle { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public ChangeRequestStatus Status { get; set; }
    public string? AdminComment { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
