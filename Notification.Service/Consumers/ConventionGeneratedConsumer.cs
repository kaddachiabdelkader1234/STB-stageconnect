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

public class ConventionGeneratedConsumer : IConsumer<ConventionGenerated>
{
    private readonly IEmailService _emailService;
    private readonly AppDbContext _dbContext;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<ConventionGeneratedConsumer> _logger;

    public ConventionGeneratedConsumer(
        IEmailService emailService,
        AppDbContext dbContext,
        IHubContext<NotificationHub> hubContext,
        ILogger<ConventionGeneratedConsumer> logger)
    {
        _emailService = emailService;
        _dbContext = dbContext;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ConventionGenerated> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Processing ConventionGenerated for {Nom} {Prenom} ({Email})",
            message.StagiaireNom, message.StagiairePrenom, message.StagiaireEmail);

        var subject = "Votre convention de stage est prête — STB";

        var template = new EmailTemplate
        {
            Title = "Convention disponible",
            Tone = EmailTone.Info,
            Icon = "📄",
            RecipientName = $"{message.StagiairePrenom} {message.StagiaireNom}",
            Intro = "Votre convention de stage a été générée et est dès maintenant " +
                    "consultable dans votre espace personnel.",
            InfoRows = new[]
            {
                new EmailInfoRow("Date de génération", $"{message.DateGeneration:dd/MM/yyyy}"),
                new EmailInfoRow("Statut", FormatStatut(message.StatutSignature))
            },
            Cta = ("Télécharger ma convention", $"{EmailTemplateBuilder.PublicUrl}/dashboard/ma-convention"),
            NextSteps = new[]
            {
                "Téléchargez le PDF et relisez-le attentivement.",
                "L'administration vous informera de la signature définitive."
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
                Type = NotificationType.ConventionGeneree,
                Message = $"Votre convention de stage a été générée. Téléchargez-la depuis votre espace personnel.",
                Lu = false,
                DateCreation = DateTime.UtcNow
            };
            _dbContext.Notifications.Add(notification);
            await _dbContext.SaveChangesAsync(context.CancellationToken);
            _logger.LogInformation("Notification record created for user {UserId}", userId);

            // Real-time push to the recipient via SignalR.
            await PushNotification(userId, notification, context.CancellationToken);
        }

        // Also notify the administration
        var adminNotif = new NotificationEntity
        {
            Id = Guid.NewGuid(),
            DestinataireId = 1,
            DestinataireRole = DestinataireRole.AdminRH,
            Type = NotificationType.ConventionGeneree,
            Message = $"Convention de stage générée pour {message.StagiairePrenom} {message.StagiaireNom}.",
            Lu = false,
            DateCreation = DateTime.UtcNow
        };
        _dbContext.Notifications.Add(adminNotif);
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        try
        {
            await _hubContext.Clients.Group("admins")
                .SendAsync("NewNotification", new
                {
                    type = "ConventionGeneree",
                    message = adminNotif.Message,
                    timestamp = adminNotif.DateCreation,
                    unread = true
                }, context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR push to admins failed for ConventionGenerated");
        }
    }

    private async Task PushNotification(long userId, NotificationEntity notification, System.Threading.CancellationToken ct)
    {
        try
        {
            await _hubContext.Clients.Group($"user:{userId}")
                .SendAsync("NewNotification", new
                {
                    type = "ConventionGeneree",
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

    private static string FormatStatut(StatutSignature statut) => statut switch
    {
        StatutSignature.EnAttente => "En attente de signature",
        StatutSignature.Signee => "Signée",
        StatutSignature.Refusee => "Refusée",
        _ => statut.ToString()
    };
}
