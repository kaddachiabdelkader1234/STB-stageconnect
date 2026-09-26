namespace Stagiaire.Contracts.Events;

/// <summary>
/// Published when an admin reviews and approves or rejects a subject change request.
/// </summary>
public record SubjectChangeReviewed(
    Guid ChangeRequestId,
    Guid StagiaireId,
    long UtilisateurId,
    Guid FinalSubjectId,
    bool Approved,
    string? Comment,
    string TraceId
);
