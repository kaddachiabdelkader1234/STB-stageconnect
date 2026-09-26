using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Notification.Service.Data;
using Notification.Service.Hubs;
using Notification.Service.Models;
using Notification.Service.Services;
using Stagiaire.Contracts.Events;
using NotificationEntity = Notification.Service.Models.Notification;

namespace Notification.Service.Consumers;

public class SubjectProposedConsumer : IConsumer<SubjectProposed>
{
    private readonly IEmailService _emailService;
    private readonly AppDbContext _dbContext;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SubjectProposedConsumer> _logger;

    public SubjectProposedConsumer(
        IEmailService emailService,
        AppDbContext dbContext,
        IHubContext<NotificationHub> hubContext,
        ILogger<SubjectProposedConsumer> logger)
    {
        _emailService = emailService;
        _dbContext = dbContext;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SubjectProposed> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Processing SubjectProposed for User {UserId}: Subject {SubjectTitle}", msg.UtilisateurId, msg.SubjectTitle);

        var recipientName = !string.IsNullOrWhiteSpace(msg.CandidateName) ? msg.CandidateName : "Cher(e) stagiaire";

        var infoRows = new List<EmailInfoRow>
        {
            new("Sujet de stage proposé", msg.SubjectTitle),
            new("Score de compatibilité IA", $"{msg.CompatibilityScore:0.##}%")
        };

        if (!string.IsNullOrWhiteSpace(msg.EncadrantNom))
        {
            infoRows.Add(new("Encadrant STB", msg.EncadrantNom));
        }

        var template = new EmailTemplate
        {
            Title = "Candidature acceptée & Sujet de stage proposé",
            Tone = EmailTone.Success,
            Icon = "🎉",
            RecipientName = recipientName,
            Intro = "Félicitations ! Nous avons le plaisir de vous informer que votre candidature de stage à la Société Tunisienne de Banque (STB) a été acceptée. Un sujet de stage vous a été affecté en adéquation avec votre profil académique et vos compétences.",
            InfoRows = infoRows.ToArray(),
            Cta = ("Consulter mon sujet de stage", $"{EmailTemplateBuilder.PublicUrl}/espace/mon-sujet"),
            NextSteps = new[]
            {
                "Connectez-vous à votre espace personnel pour consulter la fiche descriptive détaillée de votre sujet.",
                "Si vous préférez travailler sur une autre thématique, vous pouvez formuler une demande de changement de sujet directement depuis votre espace personnel.",
                "Votre convention de stage est également en cours de préparation par l'administration."
            }
        };

        var emailBody = EmailTemplateBuilder.Build(template);

        if (!string.IsNullOrWhiteSpace(msg.CandidateEmail))
        {
            try
            {
                await _emailService.SendEmailAsync(
                    msg.CandidateEmail,
                    $"Candidature acceptée — Sujet de stage proposé : {msg.SubjectTitle} — STB",
                    emailBody,
                    context.CancellationToken,
                    isHtml: true
                );
                _logger.LogInformation("Subject proposed email sent successfully to {Email}", msg.CandidateEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send subject proposed email to {Email}", msg.CandidateEmail);
            }
        }

        if (msg.UtilisateurId > 0)
        {
            var notification = new NotificationEntity
            {
                Id = Guid.NewGuid(),
                DestinataireId = msg.UtilisateurId,
                DestinataireRole = DestinataireRole.Stagiaire,
                Type = NotificationType.SujetPropose,
                Message = $"Un sujet de stage vous a été proposé : \"{msg.SubjectTitle}\" (Compatibilité : {msg.CompatibilityScore:0.##}%).",
                Lu = false,
                DateCreation = DateTime.UtcNow
            };

            _dbContext.Notifications.Add(notification);
            await _dbContext.SaveChangesAsync(context.CancellationToken);

            // SignalR push
            try
            {
                await _hubContext.Clients.Group($"user:{msg.UtilisateurId}")
                    .SendAsync("NewNotification", new
                    {
                        type = "SujetPropose",
                        message = notification.Message,
                        timestamp = notification.DateCreation,
                        unread = true
                    }, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SignalR notification push failed for user {UserId}", msg.UtilisateurId);
            }
        }
    }
}
