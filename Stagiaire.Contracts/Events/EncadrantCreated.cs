namespace Stagiaire.Contracts.Events;

/// <summary>
/// Published by auth-service (Java) when an admin creates an encadrant (TRAINER) account.
///
/// The plain-text password is carried once, in this event only: it is never stored in clear
/// and never returned by any API afterwards — it exists solely so the welcome email can hand
/// the encadrant their credentials. Auth-service publishes it raw (System.Text.Json's
/// PascalCase default matches this record) so Notification.Service deserializes it without
/// a naming-policy shim.
/// </summary>
/// <param name="UserId">auth-service <c>users.user_id</c> of the new TRAINER account.</param>
/// <param name="FirstName">Encadrant display name used in the email greeting.</param>
/// <param name="LastName">Family name, when provided at creation — omitted from the payload otherwise.</param>
/// <param name="Email">Destination address for the welcome email.</param>
/// <param name="Departement">Department the encadrant will supervise, shown in the email.</param>
/// <param name="Phone">Professional phone number, when provided — omitted from the payload otherwise.</param>
/// <param name="TemporaryPassword">Plain-text initial password — sent once, by email only.</param>
/// <param name="IsPasswordReset">True when the email re-issues credentials for an existing
/// account (admin "resend" action); the email then says the password was reset instead of
/// welcoming a new encadrant. Defaults to false so older Java publishers stay compatible.</param>
public record EncadrantCreated(
    long UserId,
    string FirstName,
    string? LastName,
    string Email,
    string Departement,
    string? Phone,
    string TemporaryPassword,
    bool IsPasswordReset = false
);
