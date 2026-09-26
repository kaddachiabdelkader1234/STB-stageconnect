using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Notification.Service.Data;
using Notification.Service.Hubs;
using Notification.Service.Models;
using Stagiaire.Contracts.Events;
using NotificationEntity = Notification.Service.Models.Notification;

namespace Notification.Service.Consumers;

public class SubjectChangeReviewedConsumer : IConsumer<SubjectChangeReviewed>
{
    private readonly AppDbContext _dbContext;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SubjectChangeReviewedConsumer> _logger;

    public SubjectChangeReviewedConsumer(
        AppDbContext dbContext,
        IHubContext<NotificationHub> hubContext,
        ILogger<SubjectChangeReviewedConsumer> logger)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SubjectChangeReviewed> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Processing SubjectChangeReviewed for User {UserId}: Approved={Approved}", msg.UtilisateurId, msg.Approved);

        var statusText = msg.Approved ? "approuvée" : "rejetée";
        var detailText = !string.IsNullOrWhiteSpace(msg.Comment) ? $" (Commentaire : {msg.Comment})" : "";

        if (msg.UtilisateurId > 0)
        {
            var notification = new NotificationEntity
            {
                Id = Guid.NewGuid(),
                DestinataireId = msg.UtilisateurId,
                DestinataireRole = DestinataireRole.Stagiaire,
                Type = NotificationType.ChangementSujetTraite,
                Message = $"Votre demande de changement de sujet a été {statusText} par l'administration{detailText}.",
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
                        type = "ChangementSujetTraite",
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
