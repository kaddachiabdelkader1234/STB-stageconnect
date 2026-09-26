using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Notification.Service.Data;
using Notification.Service.Hubs;
using Notification.Service.Models;
using Stagiaire.Contracts.Events;
using NotificationEntity = Notification.Service.Models.Notification;

namespace Notification.Service.Consumers;

public class SubjectChangeRequestedConsumer : IConsumer<SubjectChangeRequested>
{
    private readonly AppDbContext _dbContext;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SubjectChangeRequestedConsumer> _logger;

    public SubjectChangeRequestedConsumer(
        AppDbContext dbContext,
        IHubContext<NotificationHub> hubContext,
        ILogger<SubjectChangeRequestedConsumer> logger)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SubjectChangeRequested> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Processing SubjectChangeRequested from User {UserId}", msg.UtilisateurId);

        if (msg.UtilisateurId > 0)
        {
            var notification = new NotificationEntity
            {
                Id = Guid.NewGuid(),
                DestinataireId = msg.UtilisateurId,
                DestinataireRole = DestinataireRole.Stagiaire,
                Type = NotificationType.ChangementSujetDemande,
                Message = "Votre demande de changement de sujet de stage a bien été enregistrée et est en cours d'examen par l'administration.",
                Lu = false,
                DateCreation = DateTime.UtcNow
            };

            _dbContext.Notifications.Add(notification);
            await _dbContext.SaveChangesAsync(context.CancellationToken);

            try
            {
                await _hubContext.Clients.Group($"user:{msg.UtilisateurId}")
                    .SendAsync("NewNotification", new
                    {
                        type = "ChangementSujetDemande",
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

        // Also notify the administration
        var adminNotif = new NotificationEntity
        {
            Id = Guid.NewGuid(),
            DestinataireId = 1, // Primary admin
            DestinataireRole = DestinataireRole.AdminRH,
            Type = NotificationType.ChangementSujetDemande,
            Message = $"Demande de changement de sujet : Le stagiaire (ID: #{msg.UtilisateurId}) a demandé un changement de sujet. Motif : {msg.Reason}",
            Lu = false,
            DateCreation = DateTime.UtcNow
        };
        _dbContext.Notifications.Add(adminNotif);
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        try
        {
            await _hubContext.Clients.Group("admins").SendAsync("NewNotification", new
            {
                type = "ChangementSujetDemande",
                message = adminNotif.Message,
                timestamp = adminNotif.DateCreation,
                unread = true
            }, context.CancellationToken);
            await _hubContext.Clients.Group("user:1").SendAsync("NewNotification", new
            {
                type = "ChangementSujetDemande",
                message = adminNotif.Message,
                timestamp = adminNotif.DateCreation,
                unread = true
            }, context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR push to admins failed for SubjectChangeRequested");
        }
    }
}
