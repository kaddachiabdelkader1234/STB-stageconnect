using System.Net;
using System.Text;

namespace Notification.Service.Services;

/// <summary>Visual tone of the email — drives the accent color of the icon tile.</summary>
public enum EmailTone
{
    Success, // green  — acceptation, convention signée
    Danger,  // red    — rejet de candidature
    Info,    // blue   — convention générée
    Gold     // STB gold — évaluation, résultats
}

/// <summary>One label/value row in the information grid.</summary>
public record EmailInfoRow(string Label, string Value);

/// <summary>
/// Everything needed to render one branded email. Constructed by the consumers,
/// rendered by <see cref="EmailTemplateBuilder.Build"/>.
/// </summary>
public record EmailTemplate
{
    /// <summary>Big headline under the icon tile, e.g. "Candidature acceptée".</summary>
    public string Title { get; init; } = string.Empty;

    public EmailTone Tone { get; init; } = EmailTone.Info;

    /// <summary>Emoji shown inside the colored tile.</summary>
    public string Icon { get; init; } = "📨";

    /// <summary>Recipient display name, e.g. "Yasmine Ben Salah".</summary>
    public string RecipientName { get; init; } = string.Empty;

    /// <summary>Main paragraph (plain text — rendered with line breaks).</summary>
    public string Intro { get; init; } = string.Empty;

    /// <summary>Key facts rendered as a two-column grid.</summary>
    public IReadOnlyList<EmailInfoRow> InfoRows { get; init; } = Array.Empty<EmailInfoRow>();

    /// <summary>Optional emphasized box — used for the note, the rejection motif, the comment…</summary>
    public (string Label, string Value)? Highlight { get; init; }

    /// <summary>Optional call-to-action button.</summary>
    public (string Label, string Url)? Cta { get; init; }

    /// <summary>Optional "what happens next" bullet list.</summary>
    public IReadOnlyList<string> NextSteps { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Renders notification emails as a self-contained HTML document.
///
/// Constraints honored on purpose:
/// <list type="bullet">
/// <item>Table layout + inline styles only — Gmail/Outlook strip &lt;style&gt; blocks and flexbox.</item>
/// <item>Fixed 600px centered body — the most reliable width across clients.</item>
/// <item>All user-supplied values pass through <see cref="WebUtility.HtmlEncode"/>, so a name or
/// a rejection motif containing &lt;, &amp; or quotes cannot break the layout or inject markup.</item>
/// <item>Colors match the Angular app: navy #0F172A, gold #C8A951 (see tailwind.config.js).</item>
/// </list>
/// </summary>
public static class EmailTemplateBuilder
{
    private const string Navy = "#0F172A";
    private const string Gold = "#C8A951";
    private const string Ink = "#1E293B";
    private const string Muted = "#64748B";
    private const string Border = "#E2E8F0";
    private const string Background = "#F1F5F9";

    /// <summary>
    /// Frontend base URL used by the CTA buttons. Overridable per environment
    /// (set APP_PUBLIC_URL in docker-compose for a non-local deployment).
    /// </summary>
    public static string PublicUrl { get; } =
        Environment.GetEnvironmentVariable("APP_PUBLIC_URL")?.TrimEnd('/')
        ?? "http://localhost:4200";

    public static string Build(EmailTemplate t)
    {
        var (tileBg, tileFg) = t.Tone switch
        {
            EmailTone.Success => ("#DCFCE7", "#166534"),
            EmailTone.Danger  => ("#FEE2E2", "#991B1B"),
            EmailTone.Info    => ("#DBEAFE", "#1E40AF"),
            _                 => ("#F5EEDB", "#8A6D1F")
        };

        var sb = new StringBuilder(4096);
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"fr\">");
        sb.AppendLine("<head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"></head>");
        sb.AppendLine($"<body style=\"margin:0;padding:0;background:{Background};font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;color:{Ink};\">");

        // ===== Outer wrapper =====
        sb.AppendLine($"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:{Background};padding:32px 12px;\">");
        sb.AppendLine("<tr><td align=\"center\">");

        // ===== Card =====
        sb.AppendLine("<table role=\"presentation\" width=\"600\" cellpadding=\"0\" cellspacing=\"0\" style=\"width:600px;max-width:100%;background:#FFFFFF;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(15,23,42,0.08);\">");

        // Header — navy band, STB branding
        sb.AppendLine("<tr><td style=\"background:#0F172A;padding:22px 40px;\">");
        sb.AppendLine("<table role=\"presentation\" width=\"100%\"><tr>");
        sb.AppendLine($"<td style=\"color:#FFFFFF;font-size:20px;font-weight:700;letter-spacing:0.5px;\">STB <span style=\"color:{Gold};\">StageConnect</span></td>");
        sb.AppendLine($"<td align=\"right\" style=\"color:#94A3B8;font-size:12px;\">Banque &amp; Stages</td>");
        sb.AppendLine("</tr></table>");
        sb.AppendLine("</td></tr>");
        sb.AppendLine($"<tr><td style=\"height:3px;background:{Gold};font-size:0;line-height:0;\">&nbsp;</td></tr>");

        // Icon tile + title
        sb.AppendLine("<tr><td style=\"padding:36px 40px 0 40px;\">");
        sb.AppendLine($"<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\"><tr>");
        sb.AppendLine($"<td width=\"56\" height=\"56\" align=\"center\" valign=\"middle\" style=\"background:{tileBg};border-radius:14px;font-size:26px;\">{t.Icon}</td>");
        sb.AppendLine("<td style=\"padding-left:16px;\">");
        sb.AppendLine($"<div style=\"font-size:11px;font-weight:700;letter-spacing:1.5px;text-transform:uppercase;color:{Muted};\">Notification STB</div>");
        sb.AppendLine($"<div style=\"font-size:22px;font-weight:700;color:{Navy};padding-top:2px;\">{Enc(t.Title)}</div>");
        sb.AppendLine("</td></tr></table>");
        sb.AppendLine("</td></tr>");

        // Greeting + intro
        sb.AppendLine("<tr><td style=\"padding:22px 40px 0 40px;\">");
        if (!string.IsNullOrWhiteSpace(t.RecipientName))
        {
            sb.AppendLine($"<p style=\"margin:0 0 10px 0;font-size:15px;color:{Ink};\">Bonjour <strong>{Enc(t.RecipientName)}</strong>,</p>");
        }
        foreach (var line in t.Intro.Split('\n'))
        {
            sb.AppendLine($"<p style=\"margin:0 0 10px 0;font-size:15px;line-height:1.6;color:{Ink};\">{Enc(line.Trim())}</p>");
        }
        sb.AppendLine("</td></tr>");

        // Info grid
        if (t.InfoRows.Count > 0)
        {
            sb.AppendLine("<tr><td style=\"padding:12px 40px 0 40px;\">");
            sb.AppendLine($"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"border:1px solid {Border};border-radius:10px;overflow:hidden;\">");
            for (var i = 0; i < t.InfoRows.Count; i++)
            {
                var row = t.InfoRows[i];
                var bg = i % 2 == 1 ? "#F8FAFC" : "#FFFFFF";
                sb.AppendLine($"<tr><td width=\"40%\" style=\"background:{bg};padding:11px 16px;font-size:13px;color:{Muted};border-bottom:1px solid {Border};\">{Enc(row.Label)}</td>");
                sb.AppendLine($"<td style=\"background:{bg};padding:11px 16px;font-size:14px;font-weight:600;color:{Navy};border-bottom:1px solid {Border};\">{Enc(row.Value)}</td></tr>");
            }
            sb.AppendLine("</table></td></tr>");
        }

        // Highlight box (note / motif / commentaire)
        if (t.Highlight is { } hl)
        {
            var (hlBg, hlBorder) = t.Tone switch
            {
                EmailTone.Danger => ("#FEF2F2", "#FECACA"),
                EmailTone.Success => ("#F0FDF4", "#BBF7D0"),
                _ => ("#FFFBEB", "#FDE68A")
            };
            sb.AppendLine("<tr><td style=\"padding:18px 40px 0 40px;\">");
            sb.AppendLine($"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:{hlBg};border:1px solid {hlBorder};border-left:4px solid {Gold};border-radius:8px;\"><tr>");
            sb.AppendLine($"<td style=\"padding:14px 18px;\">");
            sb.AppendLine($"<div style=\"font-size:11px;font-weight:700;letter-spacing:1.2px;text-transform:uppercase;color:{Muted};padding-bottom:4px;\">{Enc(hl.Label)}</div>");
            sb.AppendLine($"<div style=\"font-size:15px;line-height:1.6;color:{Ink};\">{Enc(hl.Value)}</div>");
            sb.AppendLine("</td></tr></table></td></tr>");
        }

        // CTA button
        if (t.Cta is { } cta)
        {
            sb.AppendLine("<tr><td align=\"center\" style=\"padding:28px 40px 4px 40px;\">");
            sb.AppendLine($"<a href=\"{Enc(cta.Url)}\" style=\"display:inline-block;background:{Navy};color:#FFFFFF;text-decoration:none;font-size:15px;font-weight:600;padding:13px 34px;border-radius:8px;border:1px solid {Gold};\">{Enc(cta.Label)}</a>");
            sb.AppendLine($"<div style=\"font-size:12px;color:{Muted};padding-top:10px;\">ou copiez ce lien dans votre navigateur&nbsp;:<br><span style=\"color:{Gold};\">{Enc(cta.Url)}</span></div>");
            sb.AppendLine("</td></tr>");
        }

        // Next steps
        if (t.NextSteps.Count > 0)
        {
            sb.AppendLine("<tr><td style=\"padding:24px 40px 0 40px;\">");
            sb.AppendLine($"<div style=\"font-size:12px;font-weight:700;letter-spacing:1.2px;text-transform:uppercase;color:{Muted};padding-bottom:8px;\">Prochaines étapes</div>");
            sb.AppendLine("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\">");
            foreach (var step in t.NextSteps)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td width=\"24\" valign=\"top\" style=\"color:{Gold};font-size:14px;padding:3px 0;\">•</td>");
                sb.AppendLine($"<td style=\"font-size:14px;line-height:1.6;color:{Ink};padding:3px 0;\">{Enc(step)}</td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table></td></tr>");
        }

        // Signature
        sb.AppendLine("<tr><td style=\"padding:28px 40px 0 40px;\">");
        sb.AppendLine($"<p style=\"margin:0;font-size:14px;color:{Ink};\">Cordialement,<br><strong>L'équipe STB StageConnect</strong></p>");
        sb.AppendLine("</td></tr>");

        // Footer
        sb.AppendLine("<tr><td style=\"padding:32px 40px 0 40px;\"><table role=\"presentation\" width=\"100%\"><tr><td style=\"border-top:1px solid #E2E8F0;\"></td></tr></table></td></tr>");
        sb.AppendLine("<tr><td style=\"padding:16px 40px 28px 40px;\">");
        sb.AppendLine($"<p style=\"margin:0;font-size:12px;line-height:1.6;color:{Muted};\">Société Tunisienne de Banque — STB StageConnect<br>");
        sb.AppendLine("Ceci est un message automatique, merci de ne pas y répondre.</p>");
        sb.AppendLine($"<p style=\"margin:8px 0 0 0;font-size:11px;color:#94A3B8;\">© 2026 STB StageConnect. Tous droits réservés.</p>");
        sb.AppendLine("</td></tr>");

        sb.AppendLine("</table>"); // /card
        sb.AppendLine("</td></tr></table>"); // /wrapper
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string Enc(string value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
