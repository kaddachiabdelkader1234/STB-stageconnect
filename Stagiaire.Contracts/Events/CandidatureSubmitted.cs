namespace Stagiaire.Contracts.Events;

public record CandidatureSubmitted(
    Guid StagiaireId,
    long? UtilisateurId,
    string Nom,
    string Prenom,
    string Email,
    string? Departement,
    string? TypeStage,
    DateTime DateSoumission
);
