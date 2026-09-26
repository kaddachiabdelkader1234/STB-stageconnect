using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using Notification.Service.Data;
using NotificationEntity = Notification.Service.Models.Notification;
using Notification.Service.Models;
using Notification.Service.Services;
using Notification.Service.Hubs;
using Stagiaire.Contracts.Events;

namespace Notification.Service.Consumers;

public class EvaluationSubmittedConsumer : IConsumer<EvaluationSubmitted>
{
    private readonly IEmailService _emailService;
    private readonly AppDbContext _dbContext;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<EvaluationSubmittedConsumer> _logger;

    public EvaluationSubmittedConsumer(
        IEmailService emailService,
        AppDbContext dbContext,
        IHubContext<NotificationHub> hubContext,
        ILogger<EvaluationSubmittedConsumer> logger)
    {
        _emailService = emailService;
        _dbContext = dbContext;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EvaluationSubmitted> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Processing EvaluationSubmitted for {Nom} {Prenom} — {Type} note {Note}/20",
            message.StagiaireNom, message.StagiairePrenom, message.TypeEvaluation, message.Note);

        if (string.IsNullOrWhiteSpace(message.StagiaireEmail))
        {
            _logger.LogWarning(
                "No email address for stagiaire {StagiaireId} — skipping notification",
                message.StagiaireId);
            return;
        }

        var typeLabel = message.TypeEvaluation switch
        {
            TypeEvaluation.MiParcours => "mi-parcours",
            TypeEvaluation.Finale => "finale",
            _ => message.TypeEvaluation.ToString()
        };

        var subject = $"Résultat de votre évaluation {typeLabel} — STB";

        var infoRows = new List<EmailInfoRow>
        {
            new("Type d'évaluation", typeLabel),
            new("Note", $"{message.Note}/20")
        };
        if (message.Statut == StatutEvaluation.Validee)
        {
            infoRows.Add(new EmailInfoRow("Statut", "Validée par l'administration"));
        }

        var template = new EmailTemplate
        {
            Title = $"Évaluation {typeLabel}",
            Tone = EmailTone.Gold,
            Icon = "⭐",
            RecipientName = $"{message.StagiairePrenom} {message.StagiaireNom}",
            Intro = "Une évaluation de votre stage a été enregistrée par votre encadrant. " +
                    "Vous trouverez le détail ci-dessous.",
            InfoRows = infoRows,
            Highlight = string.IsNullOrWhiteSpace(message.Commentaire)
                ? null
                : ("Commentaire de l'encadrant", message.Commentaire),
            Cta = ("Voir mon évaluation", $"{EmailTemplateBuilder.PublicUrl}/dashboard/mes-evaluations"),
            NextSteps = new[]
            {
                "Une fois validée par l'administration, votre attestation sera disponible."
            }
        };

        var body = EmailTemplateBuilder.Build(template);

        await _emailService.SendEmailAsync(message.StagiaireEmail, subject, body, context.CancellationToken, isHtml: true);

        // Persist a Notification record so the in-app notifications screen shows it.
        if (message.UtilisateurId is { } userId)
        {
            var notification = new NotificationEntity
            {
                Id = Guid.NewGuid(),
                DestinataireId = userId,
                DestinataireRole = DestinataireRole.Stagiaire,
                Type = NotificationType.EvaluationSoumise,
                Message = $"Votre évaluation {typeLabel} a été enregistrée. Note : {message.Note}/20",
                Lu = false,
                DateCreation = DateTime.UtcNow
            };
            _dbContext.Notifications.Add(notification);
            await _dbContext.SaveChangesAsync(context.CancellationToken);
            _logger.LogInformation("Notification record created for user {UserId}", userId);

            // Real-time push to the recipient via SignalR.
            await PushNotification(userId, notification, context.CancellationToken);
        }
    }

    private async Task PushNotification(long userId, NotificationEntity notification, System.Threading.CancellationToken ct)
    {
        try
        {
            await _hubContext.Clients.Group($"user:{userId}")
                .SendAsync("NewNotification", new
                {
                    type = "EvaluationSoumise",
                    message = notification.Message,
                    timestamp = notification.DateCreation,
                    unread = true
                }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR push failed for user {UserId}", userId);
        }
    }
}
