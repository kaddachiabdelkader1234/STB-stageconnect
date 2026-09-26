using System.Text.Json;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notification.Service.Data;
using Notification.Service.Hubs;
using Notification.Service.Models;
using Notification.Service.Services;
using NotificationEntity = Notification.Service.Models.Notification;
using Stagiaire.Contracts.Events;

namespace Notification.Service.Consumers;

/// <summary>
/// Consumes CandidatureSubmitted and notifies the administration in real time and via email.
/// </summary>
public class CandidatureSubmittedConsumer : IConsumer<CandidatureSubmitted>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IEmailService _emailService;
    private readonly ILogger<CandidatureSubmittedConsumer> _logger;

    public CandidatureSubmittedConsumer(
        IHttpClientFactory httpClientFactory,
        IServiceScopeFactory scopeFactory,
        IHubContext<NotificationHub> hubContext,
        IEmailService emailService,
        ILogger<CandidatureSubmittedConsumer> logger)
    {
        _httpClientFactory = httpClientFactory;
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _emailService = emailService;
        _logger = logger;
    }

    private sealed record AdminSummary(long UserId, string Email, string FirstName, string Role);

    public async Task Consume(ConsumeContext<CandidatureSubmitted> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "CandidatureSubmitted — notifying admins: {Prenom} {Nom} ({Email}), Dept: {Dept}",
            message.Prenom, message.Nom, message.Email, message.Departement);

        var admins = await LoadAdminsAsync(context.CancellationToken);
        if (admins.Count == 0)
        {
            // Default fallback to primary admin so notifications are never dropped
            admins = new List<AdminSummary> { new(1, "admin@stb.tn", "Admin", "ADMIN") };
        }

        var dept = string.IsNullOrWhiteSpace(message.Departement) ? "Non spécifié" : message.Departement;
        var type = string.IsNullOrWhiteSpace(message.TypeStage) ? "Stage" : message.TypeStage;
        var adminMessage = $"Nouvelle candidature déposée par {message.Prenom} {message.Nom} (Département: {dept}, Type: {type}).";

        // Persist notifications for each admin + the candidate
        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 1. Admin notifications
            foreach (var admin in admins)
            {
                dbContext.Notifications.Add(new NotificationEntity
                {
                    Id = Guid.NewGuid(),
                    DestinataireId = admin.UserId,
                    DestinataireRole = DestinataireRole.AdminRH,
                    Type = NotificationType.CandidatureDeposee,
                    Message = adminMessage,
                    Lu = false,
                    DateCreation = DateTime.UtcNow
                });
            }

            // 2. Candidate notification
            if (message.UtilisateurId is { } userId && userId > 0)
            {
                var candidateMessage = $"Votre candidature de stage au département {dept} a été enregistrée avec succès. Elle est actuellement en cours d'examen par l'administration.";
                dbContext.Notifications.Add(new NotificationEntity
                {
                    Id = Guid.NewGuid(),
                    DestinataireId = userId,
                    DestinataireRole = DestinataireRole.Stagiaire,
                    Type = NotificationType.CandidatureDeposee,
                    Message = candidateMessage,
                    Lu = false,
                    DateCreation = DateTime.UtcNow
                });
            }

            await dbContext.SaveChangesAsync(context.CancellationToken);
        }

        // Live SignalR push to admins
        foreach (var admin in admins)
        {
            try
            {
                await _hubContext.Clients.Group($"user:{admin.UserId}").SendAsync("NewNotification", new
                {
                    type = "CandidatureDeposee",
                    message = adminMessage,
                    timestamp = DateTime.UtcNow,
                    unread = true
                }, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SignalR push to admin {AdminId} failed", admin.UserId);
            }
        }

        try
        {
            await _hubContext.Clients.Group("admins").SendAsync("NewNotification", new
            {
                type = "CandidatureDeposee",
                message = adminMessage,
                timestamp = DateTime.UtcNow,
                unread = true
            }, context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR push to admins group failed");
        }

        // Live SignalR push to candidate
        if (message.UtilisateurId is { } candUserId && candUserId > 0)
        {
            try
            {
                await _hubContext.Clients.Group($"user:{candUserId}").SendAsync("NewNotification", new
                {
                    type = "CandidatureDeposee",
                    message = $"Votre candidature de stage au département {dept} a été enregistrée avec succès. Elle est actuellement en cours d'examen par l'administration.",
                    timestamp = DateTime.UtcNow,
                    unread = true
                }, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SignalR push to candidate {UserId} failed", candUserId);
            }
        }

        // Send Email notification to candidate
        if (!string.IsNullOrWhiteSpace(message.Email))
        {
            try
            {
                var candidateTemplate = new EmailTemplate
                {
                    Title = "Confirmation de dépôt de candidature",
                    Tone = EmailTone.Info,
                    Icon = "📄",
                    RecipientName = $"{message.Prenom} {message.Nom}",
                    Intro = "Votre dossier de candidature de stage à la Société Tunisienne de Banque a bien été enregistré. Nos équipes des ressources humaines procèdent actuellement à son étude.",
                    InfoRows = new[]
                    {
                        new EmailInfoRow("Candidat", $"{message.Prenom} {message.Nom}"),
                        new EmailInfoRow("Département", dept),
                        new EmailInfoRow("Type de stage", type)
                    },
                    Cta = ("Suivre ma candidature", $"{EmailTemplateBuilder.PublicUrl}/espace/ma-candidature"),
                    NextSteps = new[]
                    {
                        "Votre dossier est en cours d'évaluation par la direction.",
                        "Vous recevrez une notification par email et sur votre espace dès qu'une décision aura été prise."
                    }
                };
                var candBody = EmailTemplateBuilder.Build(candidateTemplate);
                await _emailService.SendEmailAsync(
                    message.Email, "Confirmation de dépôt de candidature — STB", candBody,
                    context.CancellationToken, isHtml: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send confirmation email to candidate {Email}", message.Email);
            }
        }

        // Send Email notification to each admin
        foreach (var admin in admins)
        {
            try
            {
                var template = new EmailTemplate
                {
                    Title = "Nouvelle candidature déposée",
                    Tone = EmailTone.Gold,
                    Icon = "📋",
                    RecipientName = admin.FirstName,
                    Intro = "Un candidat vient de soumettre son dossier de candidature pour un stage à la STB.",
                    InfoRows = new[]
                    {
                        new EmailInfoRow("Candidat", $"{message.Prenom} {message.Nom}"),
                        new EmailInfoRow("Email", message.Email),
                        new EmailInfoRow("Département souhaité", dept),
                        new EmailInfoRow("Type de stage", type)
                    },
                    Cta = ("Examiner la candidature", $"{EmailTemplateBuilder.PublicUrl}/admin/candidatures")
                };

                var body = EmailTemplateBuilder.Build(template);
                await _emailService.SendEmailAsync(
                    admin.Email, "Nouvelle candidature de stage déposée — STB", body,
                    context.CancellationToken, isHtml: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send email to admin {Email}", admin.Email);
            }
        }
    }

    private async Task<List<AdminSummary>> LoadAdminsAsync(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("auth");
        foreach (var baseUri in new[] { "http://auth-service:8081", "http://localhost:8081" })
        {
            try
            {
                client.BaseAddress = new Uri(baseUri);
                var response = await client.GetAsync("/api/auth/users?role=ADMIN", ct);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("auth-service {BaseUri} returned {Status}", baseUri, response.StatusCode);
                    continue;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                var admins = await JsonSerializer.DeserializeAsync<List<AdminSummary>>(stream, cancellationToken: ct);
                if (admins is { Count: > 0 })
                {
                    return admins;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not reach auth-service at {BaseUri}", baseUri);
            }
        }
        return new List<AdminSummary>();
    }
}
