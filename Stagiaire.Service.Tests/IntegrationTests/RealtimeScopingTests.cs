using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace Stagiaire.Service.Tests.IntegrationTests;

/// <summary>
/// Proves the real-time (SignalR) channel is authenticated and scoped exactly like the REST API:
/// a learner only receives the notifications addressed to their own user id.
///
/// This is the explicit requirement from <c>MODERNIZATION_PROMPT.md</c> §1 — "the websocket
/// connection must be authenticated (reuse the JWT) and scoped the same way REST endpoints are —
/// a stagiaire must not receive push events about another stagiaire's data. Test this explicitly".
///
/// Requires the Docker stack: docker compose up -d --build
/// </summary>
[Trait("Category", "Integration")]
[Collection("Integration")]
public class RealtimeScopingTests
{
    private readonly IntegrationTestSetup _s;
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public RealtimeScopingTests(IntegrationTestSetup s) => _s = s;

    /// <summary>The hub as exposed by the gateway (NOT the notification service port).</summary>
    private static string HubUrl => $"{IntegrationTestSetup.BaseUrl}/hub/notifications";

    [Fact]
    public async Task SignalR_A_Learner_Does_Not_Receive_Another_Learners_Notifications()
    {
        var receivedByPrimary = new List<string>();
        var receivedByOther = new List<string>();

        await using var primary = BuildConnection(_s.LearnerToken);
        await using var other = BuildConnection(_s.OtherLearnerToken);

        primary.On<JsonElement>("NewNotification", n => Record(receivedByPrimary, n));
        other.On<JsonElement>("NewNotification", n => Record(receivedByOther, n));

        await primary.StartAsync();
        await other.StartAsync();

        Assert.Equal(HubConnectionState.Connected, primary.State);
        Assert.Equal(HubConnectionState.Connected, other.State);

        // Produce a notification addressed to the OTHER learner only: they submit a candidature and
        // an admin rejects it. That fires CandidatureRejected → Notification.Service pushes to
        // user:{otherLearnerId} and to nobody else.
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var submit = await PostAsTokenAsync(_s.OtherLearnerToken, "/api/v1/candidatures", new
        {
            nom = "Other",
            prenom = "Scoped",
            email = $"integ-realtime-{ts}@stb.tn",
            departement = "IT",
            typeStage = "PFE",
            ecole = "ESPRIT",
            dateDebut = "2026-09-01",
            dateFin = "2026-12-01",
            motivation = "realtime scoping test"
        });
        Assert.True(submit.IsSuccessStatusCode, $"candidature submit failed: {submit.StatusCode} {await submit.Content.ReadAsStringAsync()}");
        var candidatureId = (await submit.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetString()!;

        var reject = await PostAsAdminAsync($"/api/v1/candidatures/{candidatureId}/rejeter", new
        {
            motifRejet = "Test de scoping temps reel"
        });
        Assert.True(reject.IsSuccessStatusCode, $"reject failed: {reject.StatusCode} {await reject.Content.ReadAsStringAsync()}");

        // The addressed recipient receives it...
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline && receivedByOther.Count == 0)
        {
            await Task.Delay(250);
        }
        Assert.Contains("CandidatureRejetee", receivedByOther);

        // ...and the other learner receives nothing for the same event. This assertion is only
        // meaningful because the positive one above passed: a push definitely happened.
        Assert.Empty(receivedByPrimary);

        await primary.StopAsync();
        await other.StopAsync();
    }

    [Fact]
    public async Task SignalR_Rejects_Unauthenticated_Connections()
    {
        await using var anonymous = new HubConnectionBuilder()
            .WithUrl(HubUrl)
            .Build();

        // The hub carries [Authorize]; the handshake must fail rather than connect.
        await Assert.ThrowsAnyAsync<Exception>(() => anonymous.StartAsync());
        Assert.NotEqual(HubConnectionState.Connected, anonymous.State);
    }

    [Fact]
    public async Task SignalR_Rejects_An_Invalid_Token()
    {
        await using var forged = BuildConnection("not-a-real-jwt");

        await Assert.ThrowsAnyAsync<Exception>(() => forged.StartAsync());
        Assert.NotEqual(HubConnectionState.Connected, forged.State);
    }

    // ---- helpers ----

    private static void Record(List<string> sink, JsonElement payload)
    {
        // The pushed object is { type, message, timestamp, unread }.
        var type = payload.TryGetProperty("type", out var t) ? t.GetString() : null;
        if (type is not null)
        {
            sink.Add(type);
        }
    }

    /// <summary>
    /// Builds an authenticated hub connection. Browsers cannot set an Authorization header on a
    /// WebSocket, so the JWT travels as the <c>access_token</c> query parameter — which the gateway
    /// translates into a bearer header (WebSocketJwtFilter) and the hub validates.
    /// </summary>
    private static HubConnection BuildConnection(string token) =>
        new HubConnectionBuilder()
            .WithUrl(HubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
                options.SkipNegotiation = true;
            })
            .Build();

    private async Task<HttpResponseMessage> PostAsTokenAsync(string token, string path, object body)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body, options: _json)
        };
        req.Headers.Authorization = new("Bearer", token);
        return await new HttpClient { BaseAddress = new Uri(IntegrationTestSetup.BaseUrl) }.SendAsync(req);
    }

    private async Task<HttpResponseMessage> PostAsAdminAsync(string path, object body)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body, options: _json)
        };
        req.Headers.Authorization = new("Bearer", _s.AdminToken);
        return await new HttpClient { BaseAddress = new Uri(IntegrationTestSetup.BaseUrl) }.SendAsync(req);
    }
}
