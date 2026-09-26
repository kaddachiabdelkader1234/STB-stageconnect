using MassTransit;
using Microsoft.Extensions.Logging;
using Notification.Service.Services;
using Stagiaire.Contracts.Events;

namespace Notification.Service.Consumers;

/// <summary>
/// Consumes EncadrantCreated (published raw-JSON by auth-service onto the smartek.events
/// topic exchange) and emails the new encadrant a branded welcome message containing their
/// login credentials.
///
/// The password travels only in this event and in the email — it is never persisted and
/// never returned by any API. The consumer deserializes the Java-side map directly: Jackson
/// emits PascalCase keys, which is also System.Text.Json's default, so no naming-policy shim
/// is needed.
/// </summary>
public class EncadrantCreatedConsumer : IConsumer<EncadrantCreated>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EncadrantCreatedConsumer> _logger;

    public EncadrantCreatedConsumer(IEmailService emailService, ILogger<EncadrantCreatedConsumer> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EncadrantCreated> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Processing EncadrantCreated for {FirstName} ({Email}) — sending welcome email",
            message.FirstName, message.Email);

        // When the admin re-sends credentials for an existing account, the message says the
        // password was reset instead of welcoming a brand-new encadrant.
        var isReset = message.IsPasswordReset;

        var subject = isReset
            ? "Vos identifiants de connexion — plateforme STB (mot de passe réinitialisé)"
            : "Bienvenue sur la plateforme STB — vos identifiants d'encadrant";

        var template = new EmailTemplate
        {
            Title = isReset ? "Vos identifiants ont été réinitialisés" : "Bienvenue, Encadrant",
            Tone = EmailTone.Info,
            Icon = "🎓",
            RecipientName = string.IsNullOrWhiteSpace(message.LastName)
                ? message.FirstName
                : $"{message.FirstName} {message.LastName}",
            Intro = isReset
                ? "Un administrateur a réinitialisé vos identifiants sur la plateforme de " +
                  "gestion des stagiaires de la Société Tunisienne de Banque. Votre nouveau " +
                  "mot de passe est indiqué ci-dessous — vos identifiants précédents ne " +
                  "sont plus valides."
                : "Votre compte encadrant vient d'être créé sur la plateforme de gestion " +
                  "des stagiaires de la Société Tunisienne de Banque. Retrouvez vos " +
                  "identifiants de connexion ci-dessous.",
            InfoRows = new List<EmailInfoRow>
                {
                    new("Adresse email", message.Email),
                    new("Mot de passe", message.TemporaryPassword)
                }
                .Concat(string.IsNullOrWhiteSpace(message.Departement)
                    ? Array.Empty<EmailInfoRow>()
                    : new[] { new EmailInfoRow("Département", message.Departement) })
                .Concat(string.IsNullOrWhiteSpace(message.Phone)
                    ? Array.Empty<EmailInfoRow>()
                    : new[] { new EmailInfoRow("Téléphone", message.Phone) })
                .ToArray(),
            Cta = ("Se connecter à mon espace", $"{EmailTemplateBuilder.PublicUrl}/auth/sign-in"),
            NextSteps = isReset
                ? new[]
                {
                    "Connectez-vous avec l'email et le nouveau mot de passe indiqués ci-dessus.",
                    "Votre ancien mot de passe ne fonctionne plus.",
                    "Conservez ce mot de passe en lieu sûr."
                }
                : new[]
                {
                    "Connectez-vous avec l'email et le mot de passe indiqués ci-dessus.",
                    "Ce mot de passe est définitif : conservez-le en lieu sûr.",
                    "Vous y suivrez vos stagiaires, leurs journaux hebdomadaires et leurs évaluations."
                }
        };

        var body = EmailTemplateBuilder.Build(template);

        await _emailService.SendEmailAsync(message.Email, subject, body, context.CancellationToken, isHtml: true);

        _logger.LogInformation("Welcome email sent to {Email}", message.Email);
    }
}
