namespace Stagiaire.Contracts.Events;

/// <summary>
/// Published when a user requests a password reset link.
/// Consumed by Notification.Service to send an email with the secure reset link.
/// </summary>
public record PasswordResetRequested(
    long UserId,
    string Email,
    string FirstName,
    string LastName,
    string ResetToken,
    string ResetUrl,
    DateTime ExpiresAt,
    string TraceId);
