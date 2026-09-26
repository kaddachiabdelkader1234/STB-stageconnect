using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SubjectMatching.Service.Models;

[Table("candidate_profiles")]
public class CandidateProfile
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid StagiaireId { get; set; }

    public long UtilisateurId { get; set; }

    [MaxLength(100)]
    public string? CvFileHash { get; set; }

    [Column(TypeName = "text")]
    public string? Education { get; set; }

    [Column(TypeName = "jsonb")]
    public List<string> Skills { get; set; } = new();

    [Column(TypeName = "text")]
    public string? Experience { get; set; }

    [Column(TypeName = "jsonb")]
    public List<string> Projects { get; set; } = new();

    [Column(TypeName = "jsonb")]
    public List<string> Languages { get; set; } = new();

    public DateTime ParsedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string Provider { get; set; } = "mock";
}
