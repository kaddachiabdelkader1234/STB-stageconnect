using MassTransit;
using Microsoft.Extensions.Logging;
using Notification.Service.Services;
using Stagiaire.Contracts.Events;

namespace Notification.Service.Consumers;

/// <summary>
/// Consumes PasswordResetRequested (published by auth-service) and sends
/// a secure link allowing the user to choose their new password.
/// </summary>
public class PasswordResetRequestedConsumer : IConsumer<PasswordResetRequested>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<PasswordResetRequestedConsumer> _logger;

    public PasswordResetRequestedConsumer(IEmailService emailService, ILogger<PasswordResetRequestedConsumer> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PasswordResetRequested> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Processing PasswordResetRequested for {Email} — sending reset link",
            message.Email);

        var recipientName = string.IsNullOrWhiteSpace(message.LastName)
            ? (string.IsNullOrWhiteSpace(message.FirstName) ? "Utilisateur STB" : message.FirstName)
            : $"{message.FirstName} {message.LastName}";

        var template = new EmailTemplate
        {
            Title = "Réinitialisation de votre mot de passe",
            Tone = EmailTone.Info,
            Icon = "🔒",
            RecipientName = recipientName,
            Intro = "Vous avez demandé la réinitialisation de votre mot de passe pour votre compte sur la plateforme STB Gestion des Stagiaires.",
            InfoRows = new[]
            {
                new EmailInfoRow("Compte concerné", message.Email),
                new EmailInfoRow("Validité du lien", "30 minutes")
            },
            Cta = ("Réinitialiser mon mot de passe", message.ResetUrl),
            NextSteps = new[]
            {
                "Cliquez sur le bouton ci-dessus pour définir votre nouveau mot de passe.",
                "Ce lien sécurisé est à usage unique et expirera dans 30 minutes.",
                "Si vous n'êtes pas à l'origine de cette demande, vous pouvez ignorer cet email en toute sécurité : votre mot de passe actuel reste inchangé."
            }
        };

        var body = EmailTemplateBuilder.Build(template);

        await _emailService.SendEmailAsync(
            message.Email,
            "Réinitialisation de votre mot de passe — Plateforme STB",
            body,
            context.CancellationToken,
            isHtml: true);

        _logger.LogInformation("Password reset link email successfully sent to {Email}", message.Email);
    }
}
