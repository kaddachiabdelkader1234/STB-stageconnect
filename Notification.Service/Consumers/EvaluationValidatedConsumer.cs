using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Notification.Service.Data;
using Notification.Service.Hubs;
using Notification.Service.Models;
using Notification.Service.Services;
using NotificationEntity = Notification.Service.Models.Notification;
using Stagiaire.Contracts.Events;

namespace Notification.Service.Consumers;

/// <summary>
/// Consumes EvaluationValidated (published by Evaluation.Service when an admin approves an
/// evaluation) and notifies both parties:
///   • the learner — in-app notification + SignalR popup + branded email: their result is
///     now official and visible on their dashboard;
///   • the encadrant — in-app notification + SignalR popup: their grading was approved.
///
/// The learner's copy of an evaluation only becomes visible to them after validation, so this
/// is the moment they are told to go look.
/// </summary>
public class EvaluationValidatedConsumer : IConsumer<EvaluationValidated>
{
    private readonly IEmailService _emailService;
    private readonly AppDbContext _dbContext;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<EvaluationValidatedConsumer> _logger;

    public EvaluationValidatedConsumer(
        IEmailService emailService,
        AppDbContext dbContext,
        IHubContext<NotificationHub> hubContext,
        ILogger<EvaluationValidatedConsumer> logger)
    {
        _emailService = emailService;
        _dbContext = dbContext;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EvaluationValidated> context)
    {
        var message = context.Message;

        var typeLabel = message.TypeEvaluation switch
        {
            TypeEvaluation.MiParcours => "mi-parcours",
            TypeEvaluation.Finale => "finale",
            _ => message.TypeEvaluation.ToString()
        };

        _logger.LogInformation(
            "Processing EvaluationValidated for {Prenom} {Nom} — {Type} note {Note}/20",
            message.StagiairePrenom, message.StagiaireNom, typeLabel, message.Note);

        // ---- 1. The learner: in-app + popup + email ----
        if (message.UtilisateurId is { } learnerId)
        {
            await NotifyAsync(
                destinataireId: learnerId,
                destinataireRole: DestinataireRole.Stagiaire,
                type: NotificationType.EvaluationValidee,
                messageText: $"Votre évaluation {typeLabel} a été validée par l'administration. Note finale : {(int)message.Note}/20",
                signalRType: "EvaluationValidee",
                context.CancellationToken);

            if (!string.IsNullOrWhiteSpace(message.StagiaireEmail))
            {
                await SendLearnerEmailAsync(message, typeLabel, context.CancellationToken);
            }
        }

        // ---- 2. The encadrant: in-app + popup (no email — they see it live) ----
        if (message.EncadrantId is { } encadrantId)
        {
            await NotifyAsync(
                destinataireId: encadrantId,
                destinataireRole: DestinataireRole.Encadrant,
                type: NotificationType.EvaluationValidee,
                messageText: $"L'évaluation {typeLabel} de {message.StagiairePrenom} {message.StagiaireNom} a été validée par l'administration.",
                signalRType: "EvaluationValidee",
                context.CancellationToken);
        }
    }

    private async Task SendLearnerEmailAsync(EvaluationValidated message, string typeLabel, CancellationToken ct)
    {
        var subject = $"Votre évaluation {typeLabel} est validée — STB";

        var template = new EmailTemplate
        {
            Title = "Évaluation validée",
            Tone = EmailTone.Success,
            Icon = "✅",
            RecipientName = $"{message.StagiairePrenom} {message.StagiaireNom}",
            Intro = "Bonne nouvelle ! L'administration de la Société Tunisienne de Banque a validé " +
                    "votre évaluation de stage. Votre résultat est désormais officiel.",
            InfoRows = new[]
            {
                new EmailInfoRow("Type d'évaluation", typeLabel),
                new EmailInfoRow("Note finale", $"{message.Note}/20"),
                new EmailInfoRow("Statut", "Validée par l'administration")
            },
            Cta = ("Voir mon évaluation", $"{EmailTemplateBuilder.PublicUrl}/dashboard/mes-evaluations"),
            NextSteps = new[]
            {
                "Votre attestation sera disponible une fois le stage terminé.",
                "Consultez votre espace pour le détail complet de l'évaluation."
            }
        };

        var body = EmailTemplateBuilder.Build(template);
        await _emailService.SendEmailAsync(message.StagiaireEmail, subject, body, ct, isHtml: true);
    }

    /// <summary>
    /// Persists one notification row and pushes it over SignalR. Shared by the learner and
    /// encadrant branches — both get the same shaped payload so the frontend popup logic is
    /// identical for every role.
    /// </summary>
    private async Task NotifyAsync(
        long destinataireId,
        DestinataireRole destinataireRole,
        NotificationType type,
        string messageText,
        string signalRType,
        CancellationToken ct)
    {
        var notification = new NotificationEntity
        {
            Id = Guid.NewGuid(),
            DestinataireId = destinataireId,
            DestinataireRole = destinataireRole,
            Type = type,
            Message = messageText,
            Lu = false,
            DateCreation = DateTime.UtcNow
        };
        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync(ct);

        await PushNotification(destinataireId, signalRType, messageText, notification.DateCreation, ct);
        _logger.LogInformation(
            "EvaluationValidated notification created for {Role} {UserId}",
            destinataireRole, destinataireId);
    }

    private async Task PushNotification(long userId, string type, string message, DateTime timestamp, CancellationToken ct)
    {
        try
        {
            await _hubContext.Clients.Group($"user:{userId}")
                .SendAsync("NewNotification", new
                {
                    type,
                    message,
                    timestamp,
                    unread = true
                }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR push failed for user {UserId}", userId);
        }
    }
}
