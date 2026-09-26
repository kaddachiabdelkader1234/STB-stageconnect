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

public class CandidatureRejectedConsumer : IConsumer<CandidatureRejected>
{
    private readonly IEmailService _emailService;
    private readonly AppDbContext _dbContext;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<CandidatureRejectedConsumer> _logger;

    public CandidatureRejectedConsumer(
        IEmailService emailService,
        AppDbContext dbContext,
        IHubContext<NotificationHub> hubContext,
        ILogger<CandidatureRejectedConsumer> logger)
    {
        _emailService = emailService;
        _dbContext = dbContext;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CandidatureRejected> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Processing CandidatureRejected for {Nom} {Prenom} ({Email})",
            message.Nom, message.Prenom, message.Email);

        var subject = "Votre candidature n'a pas été retenue — STB";

        var template = new EmailTemplate
        {
            Title = "Candidature non retenue",
            Tone = EmailTone.Danger,
            Icon = "✉️",
            RecipientName = $"{message.Prenom} {message.Nom}",
            Intro = "Après examen de votre candidature, nous avons le regret de vous informer " +
                    "qu'elle n'a pas été retenue pour cette session de stage.",
            Highlight = ("Motif", message.MotifRejet),
            NextSteps = new[]
            {
                "Vous pouvez soumettre une nouvelle candidature pour une prochaine session.",
                "Votre espace personnel reste accessible à tout moment."
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
                Type = NotificationType.CandidatureRejetee,
                Message = $"Votre candidature de stage n'a pas été retenue. Motif : {message.MotifRejet}",
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
                    type = "CandidatureRejetee",
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
