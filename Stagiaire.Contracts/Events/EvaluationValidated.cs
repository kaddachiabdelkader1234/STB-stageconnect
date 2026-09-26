namespace Stagiaire.Contracts.Events;

/// <summary>
/// Published by Evaluation.Service when an admin validates an evaluation
/// (POST /api/v1/evaluations/{id}/valider).
///
/// Carries everything Notification.Service needs to tell the learner their result is official
/// and the encadrant that their grading was approved — the learner's own copy of the result
/// only becomes visible once the administration has validated it.
/// </summary>
/// <param name="EvaluationId">Id of the validated evaluation row.</param>
/// <param name="StagiaireId">Guid of the stagiaire record the evaluation is about.</param>
/// <param name="StagiaireNom">Stagiaire last name — email greeting and message text.</param>
/// <param name="StagiairePrenom">Stagiaire first name.</param>
/// <param name="StagiaireEmail">
/// Destination for the learner's "your result is official" email; empty when the record has
/// no address, in which case the consumer skips the email but still pushes in-app.
/// </param>
/// <param name="UtilisateurId">
/// auth-service user id of the learner — in-app notification + SignalR push recipient.
/// </param>
/// <param name="EncadrantId">
/// auth-service user id of the encadrant who graded the stagiaire — in-app notification +
/// SignalR push recipient, so they know their evaluation was approved.
/// </param>
/// <param name="TypeEvaluation">MiParcours or Finale.</param>
/// <param name="Note">Grade out of 20.</param>
public record EvaluationValidated(
    Guid EvaluationId,
    Guid StagiaireId,
    string StagiaireNom,
    string StagiairePrenom,
    string StagiaireEmail,
    long? UtilisateurId,
    long? EncadrantId,
    TypeEvaluation TypeEvaluation,
    decimal Note,
    string? TraceId = null
);
