using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Notification.Service.Data;
using Notification.Service.Hubs;
using Notification.Service.Models;
using Stagiaire.Contracts.Events;
using NotificationEntity = Notification.Service.Models.Notification;

namespace Notification.Service.Consumers;

public class SubjectAcceptedConsumer : IConsumer<SubjectAccepted>
{
    private readonly AppDbContext _dbContext;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SubjectAcceptedConsumer> _logger;

    public SubjectAcceptedConsumer(
        AppDbContext dbContext,
        IHubContext<NotificationHub> hubContext,
        ILogger<SubjectAcceptedConsumer> logger)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SubjectAccepted> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Processing SubjectAccepted by User {UserId} for Subject {SubjectTitle}", msg.UtilisateurId, msg.SubjectTitle);

        if (msg.UtilisateurId > 0)
        {
            var notification = new NotificationEntity
            {
                Id = Guid.NewGuid(),
                DestinataireId = msg.UtilisateurId,
                DestinataireRole = DestinataireRole.Stagiaire,
                Type = NotificationType.SujetAccepte,
                Message = $"Félicitations ! Vous avez accepté le sujet de stage \"{msg.SubjectTitle}\". Votre affectation est confirmée.",
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
                        type = "SujetAccepte",
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
            DestinataireId = 1,
            DestinataireRole = DestinataireRole.AdminRH,
            Type = NotificationType.SujetAccepte,
            Message = $"Sujet validé : Le stagiaire (ID: #{msg.UtilisateurId}) a accepté le sujet \"{msg.SubjectTitle}\".",
            Lu = false,
            DateCreation = DateTime.UtcNow
        };
        _dbContext.Notifications.Add(adminNotif);
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        try
        {
            await _hubContext.Clients.Group("admins").SendAsync("NewNotification", new
            {
                type = "SujetAccepte",
                message = adminNotif.Message,
                timestamp = adminNotif.DateCreation,
                unread = true
            }, context.CancellationToken);
            await _hubContext.Clients.Group("user:1").SendAsync("NewNotification", new
            {
                type = "SujetAccepte",
                message = adminNotif.Message,
                timestamp = adminNotif.DateCreation,
                unread = true
            }, context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR push to admins failed for SubjectAccepted");
        }
    }
}
