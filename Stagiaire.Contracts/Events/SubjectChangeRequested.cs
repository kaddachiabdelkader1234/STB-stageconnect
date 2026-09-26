namespace Stagiaire.Contracts.Events;

/// <summary>
/// Published when a candidate/stagiaire requests a different internship subject.
/// </summary>
public record SubjectChangeRequested(
    Guid ChangeRequestId,
    Guid StagiaireId,
    long UtilisateurId,
    Guid CurrentSubjectId,
    Guid RequestedSubjectId,
    string Reason,
    string TraceId
);
