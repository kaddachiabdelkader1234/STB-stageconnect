namespace Stagiaire.Contracts.Events;

/// <summary>
/// Published when an admin proposes an internship subject to a candidate/stagiaire.
/// </summary>
public record SubjectProposed(
    Guid StagiaireId,
    long UtilisateurId,
    Guid SubjectId,
    string SubjectTitle,
    decimal CompatibilityScore,
    string TraceId,
    string? CandidateEmail = null,
    string? CandidateName = null,
    string? EncadrantNom = null
);
