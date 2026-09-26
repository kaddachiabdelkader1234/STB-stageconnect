namespace Stagiaire.Contracts.Events;

/// <summary>
/// Published when a candidate/stagiaire accepts a proposed internship subject.
/// </summary>
public record SubjectAccepted(
    Guid StagiaireId,
    long UtilisateurId,
    Guid SubjectId,
    string SubjectTitle,
    string TraceId
);
