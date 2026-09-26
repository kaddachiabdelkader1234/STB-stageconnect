using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SubjectMatching.Service.Models;

[Table("subject_change_requests")]
public class SubjectChangeRequest
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid AssignmentId { get; set; }

    [Required]
    public Guid StagiaireId { get; set; }

    public long UtilisateurId { get; set; }

    [Required]
    public Guid CurrentSubjectId { get; set; }

    [Required]
    public Guid RequestedSubjectId { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Reason { get; set; } = string.Empty;

    public ChangeRequestStatus Status { get; set; } = ChangeRequestStatus.Pending;

    [MaxLength(2000)]
    public string? AdminComment { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }

    [ForeignKey(nameof(CurrentSubjectId))]
    public InternshipSubject? CurrentSubject { get; set; }

    [ForeignKey(nameof(RequestedSubjectId))]
    public InternshipSubject? RequestedSubject { get; set; }

    [ForeignKey(nameof(AssignmentId))]
    public SubjectAssignment? Assignment { get; set; }
}
