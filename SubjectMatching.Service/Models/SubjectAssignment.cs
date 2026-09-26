using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SubjectMatching.Service.Models;

[Table("subject_assignments")]
public class SubjectAssignment
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid StagiaireId { get; set; }

    public long UtilisateurId { get; set; }

    [Required]
    public Guid SubjectId { get; set; }

    public AssignmentStatus Status { get; set; } = AssignmentStatus.Proposed;

    [Column(TypeName = "decimal(5,2)")]
    public decimal ScoreAtProposal { get; set; }

    public DateTime ProposedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RespondedAt { get; set; }

    [ForeignKey(nameof(SubjectId))]
    public InternshipSubject? Subject { get; set; }
}
