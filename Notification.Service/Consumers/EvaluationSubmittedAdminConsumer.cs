using System.Text.Json;
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
/// Consumes EvaluationSubmitted and ALSO notifies the administration.
///
/// The existing per-learner email/notification stays in <see cref="EvaluationSubmittedConsumer"/>;
/// this consumer adds the other half of the workflow the UI implies: an evaluation lands on
/// <c>Soumise</c> and only an admin can validate it — so admins must know it is waiting.
///
/// The admin roster comes from auth-service's internal endpoint
/// (<c>GET /api/auth/users?role=ADMIN</c> — reachable without a token inside the compose
/// network, never exposed through the gateway). Each admin gets an in-app notification
/// persisted for the panel, a live SignalR popup via the "admins" group, and an email.
/// </summary>
public class EvaluationSubmittedAdminConsumer : IConsumer<EvaluationSubmitted>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IEmailService _emailService;
    private readonly ILogger<EvaluationSubmittedAdminConsumer> _logger;

    public EvaluationSubmittedAdminConsumer(
        IHttpClientFactory httpClientFactory,
        IServiceScopeFactory scopeFactory,
        IHubContext<NotificationHub> hubContext,
        IEmailService emailService,
        ILogger<EvaluationSubmittedAdminConsumer> logger)
    {
        _httpClientFactory = httpClientFactory;
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _emailService = emailService;
        _logger = logger;
    }

    private sealed record AdminSummary(long UserId, string Email, string FirstName, string Role);

    public async Task Consume(ConsumeContext<EvaluationSubmitted> context)
    {
        var message = context.Message;

        var typeLabel = message.TypeEvaluation switch
        {
            TypeEvaluation.MiParcours => "mi-parcours",
            TypeEvaluation.Finale => "finale",
            _ => message.TypeEvaluation.ToString()
        };

        _logger.LogInformation(
            "EvaluationSubmitted — notifying admins: {Prenom} {Nom}, {Type}, note {Note}/20",
            message.StagiairePrenom, message.StagiaireNom, typeLabel, message.Note);

        var admins = await LoadAdminsAsync(context.CancellationToken);
        if (admins.Count == 0)
        {
            _logger.LogWarning("No ADMIN accounts returned by auth-service — admin notification skipped");
            return;
        }

        var adminMessage =
            $"Nouvelle évaluation {typeLabel} de {message.StagiairePrenom} {message.StagiaireNom} " +
            $"(note {message.Note}/20) en attente de votre validation.";

        // Persist one row per admin so the notification panel shows it after a reconnect, and
        // push live so any admin online sees the popup immediately.
        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            foreach (var admin in admins)
            {
                dbContext.Notifications.Add(new NotificationEntity
                {
                    Id = Guid.NewGuid(),
                    DestinataireId = admin.UserId,
                    DestinataireRole = DestinataireRole.AdminRH,
                    Type = NotificationType.EvaluationSoumise,
                    Message = adminMessage,
                    Lu = false,
                    DateCreation = DateTime.UtcNow
                });
            }
            await dbContext.SaveChangesAsync(context.CancellationToken);
        }

        foreach (var admin in admins)
        {
            try
            {
                await _hubContext.Clients.Group($"user:{admin.UserId}").SendAsync("NewNotification", new
                {
                    type = "EvaluationSoumise",
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

        // Broadcast to the joined "admins" group too — covers admins logged in on several tabs.
        try
        {
            await _hubContext.Clients.Group("admins").SendAsync("NewNotification", new
            {
                type = "EvaluationSoumise",
                message = adminMessage,
                timestamp = DateTime.UtcNow,
                unread = true
            }, context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR push to admins group failed");
        }

        // Email each admin so validation does not require being online.
        foreach (var admin in admins)
        {
            var template = new EmailTemplate
            {
                Title = "Validation requise",
                Tone = EmailTone.Gold,
                Icon = "📝",
                RecipientName = admin.FirstName,
                Intro = "Un encadrant vient d'enregistrer une évaluation de stage. " +
                        "Elle est en attente de votre validation.",
                InfoRows = new[]
                {
                    new EmailInfoRow("Stagiaire", $"{message.StagiairePrenom} {message.StagiaireNom}"),
                    new EmailInfoRow("Type", typeLabel),
                    new EmailInfoRow("Note proposée", $"{message.Note}/20")
                },
                Cta = ("Ouvrir le tableau de bord admin", $"{EmailTemplateBuilder.PublicUrl}/dashboard/evaluations")
            };
            var body = EmailTemplateBuilder.Build(template);
            await _emailService.SendEmailAsync(
                admin.Email, "Évaluation en attente de validation — STB", body,
                context.CancellationToken, isHtml: true);
        }
    }

    /// <summary>
    /// Fetches the ADMIN roster from auth-service. Two URL candidates because the container's
    /// view of the world differs from a dev machine: docker-compose DNS name first, then
    /// localhost for anyone running the stack bare-metal.
    /// </summary>
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
