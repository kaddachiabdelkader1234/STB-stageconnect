using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SubjectMatching.Service.Models;

[Table("subject_matches")]
public class SubjectMatch
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid StagiaireId { get; set; }

    [Required]
    public Guid SubjectId { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal CompatibilityScore { get; set; }

    [Column(TypeName = "jsonb")]
    public List<string> MatchedSkills { get; set; } = new();

    [Column(TypeName = "jsonb")]
    public List<string> MissingSkills { get; set; } = new();

    [Column(TypeName = "text")]
    public string Explanation { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SubjectId))]
    public InternshipSubject? Subject { get; set; }
}
