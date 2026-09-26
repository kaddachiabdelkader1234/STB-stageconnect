using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Notification.Service.Hubs;

/// <summary>
/// WebSocket hub for real-time notifications (SignalR).
/// Authenticated users are grouped by their JWT <c>sub</c> (userId) claim
/// so broadcasts are scoped to the recipient. Connections authenticate via JWT
/// passed either as a bearer token (Authorization header) or as the
/// <c>access_token</c> query parameter (required for WebSocket upgrade since
/// browsers cannot set custom headers on WebSocket connections).
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    /// <summary>
    /// Client joins a per-user group so broadcasts are scoped to the recipient.
    /// The user id is extracted from the JWT <c>sub</c> claim, which
    /// <c>JwtAuthenticationExtensions</c> maps to <c>NameClaimType = "sub"</c>.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("userId")?.Value;
        var sub = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        }

        if (!string.IsNullOrEmpty(sub) && sub != userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{sub}");
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Allow admins to join a broadcast group for admin-only dashboard events.
    /// </summary>
    public async Task JoinAdminGroup()
    {
        if (Context.User?.IsInRole("ADMIN") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
        }
    }
}
