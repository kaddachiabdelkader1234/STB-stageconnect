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

public class CandidatureAcceptedConsumer : IConsumer<CandidatureAccepted>
{
    private readonly IEmailService _emailService;
    private readonly AppDbContext _dbContext;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<CandidatureAcceptedConsumer> _logger;

    public CandidatureAcceptedConsumer(
        IEmailService emailService,
        AppDbContext dbContext,
        IHubContext<NotificationHub> hubContext,
        ILogger<CandidatureAcceptedConsumer> logger)
    {
        _emailService = emailService;
        _dbContext = dbContext;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CandidatureAccepted> context)
    {
        var message = context.Message;

        // Propagate the trace ID from the originating HTTP request into this service's logs.
        using var scope = !string.IsNullOrEmpty(message.TraceId)
            ? _logger.BeginScope(new Dictionary<string, object?> { ["TraceId"] = message.TraceId })
            : null;

        _logger.LogInformation(
            "Processing CandidatureAccepted for {Nom} {Prenom} ({Email})",
            message.Nom, message.Prenom, message.Email);

        var subject = "Votre candidature a été acceptée — STB";

        var template = new EmailTemplate
        {
            Title = "Candidature acceptée",
            Tone = EmailTone.Success,
            Icon = "🎉",
            RecipientName = $"{message.Prenom} {message.Nom}",
            Intro = "Nous avons le plaisir de vous informer que votre candidature de stage " +
                    "à la Société Tunisienne de Banque a été acceptée. Félicitations !",
            InfoRows = new[]
            {
                new EmailInfoRow("Département", message.Departement),
                new EmailInfoRow("Dates du stage",
                    $"{message.DateDebut:dd/MM/yyyy} au {message.DateFin:dd/MM/yyyy}")
            },
            Cta = ("Accéder à mon espace", $"{EmailTemplateBuilder.PublicUrl}/dashboard/ma-convention"),
            NextSteps = new[]
            {
                "Votre convention de stage sera générée automatiquement.",
                "Consultez également le sujet de stage qui vous a été attribué dans la rubrique 'Mon Sujet'. Si vous souhaitez une autre thématique, vous pouvez déposer une demande de changement directement depuis votre espace."
            }
        };

        var body = EmailTemplateBuilder.Build(template);

        await _emailService.SendEmailAsync(message.Email, subject, body, context.CancellationToken, isHtml: true);

        // Persist a Notification record so the in-app notifications screen shows it.
        if (message.UtilisateurId is { } userId)
        {
            var notification = new NotificationEntity
            {
                Id = Guid.NewGuid(),
                DestinataireId = userId,
                DestinataireRole = DestinataireRole.Stagiaire,
                Type = NotificationType.CandidatureAcceptee,
                Message = $"Votre candidature de stage au département {message.Departement} a été acceptée.",
                Lu = false,
                DateCreation = DateTime.UtcNow
            };
            _dbContext.Notifications.Add(notification);
            await _dbContext.SaveChangesAsync(context.CancellationToken);
            _logger.LogInformation("Notification record created for user {UserId}", userId);

            // Real-time push to the recipient via SignalR.
            await PushNotification(userId, notification, context.CancellationToken);
        }

        // Also notify the assigned Encadrant (Trainer)
        if (message.EncadrantId is { } encadrantId && encadrantId > 0)
        {
            var encMessage = $"Nouveau stagiaire attribué : {message.Prenom} {message.Nom} vous a été assigné pour son stage au département {message.Departement} ({message.DateDebut:dd/MM/yyyy} au {message.DateFin:dd/MM/yyyy}).";
            var encNotification = new NotificationEntity
            {
                Id = Guid.NewGuid(),
                DestinataireId = encadrantId,
                DestinataireRole = DestinataireRole.Encadrant,
                Type = NotificationType.CandidatureAcceptee,
                Message = encMessage,
                Lu = false,
                DateCreation = DateTime.UtcNow
            };
            _dbContext.Notifications.Add(encNotification);
            await _dbContext.SaveChangesAsync(context.CancellationToken);
            _logger.LogInformation("Notification record created for encadrant {EncadrantId}", encadrantId);

            await PushNotification(encadrantId, encNotification, context.CancellationToken);
        }
    }

    private async Task PushNotification(long userId, NotificationEntity notification, System.Threading.CancellationToken ct)
    {
        try
        {
            await _hubContext.Clients.Group($"user:{userId}")
                .SendAsync("NewNotification", new
                {
                    type = "CandidatureAcceptee",
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
