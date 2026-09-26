using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SubjectMatching.Service.Models;

[Table("internship_subjects")]
public class InternshipSubject
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

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

    /// <summary>
    /// Stored as JSON string in PostgreSQL.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public List<string> RequiredSkills { get; set; } = new();

    /// <summary>
    /// Stored as JSON string in PostgreSQL.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public List<string> PreferredSkills { get; set; } = new();

    [MaxLength(500)]
    public string? EducationRequirements { get; set; }

    [MaxLength(500)]
    public string? ExperienceRequirements { get; set; }

    public SubjectDifficulty Difficulty { get; set; } = SubjectDifficulty.Intermediate;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public int AvailablePositions { get; set; } = 1;

    public int FilledPositions { get; set; } = 0;

    public SubjectStatus Status { get; set; } = SubjectStatus.Open;

    public long CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
