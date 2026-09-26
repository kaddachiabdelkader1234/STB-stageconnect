using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Stagiaire.Service.Tests.IntegrationTests;

/// <summary>
/// Covers the event consumers that were previously only verified by reading logs:
/// <c>ConventionGeneratedConsumer</c> and <c>CandidatureRejectedConsumer</c>. Each consumer
/// persists a Notification row, which is observable through the admin notifications API — so an
/// assertion on that row is real evidence the consumer ran end to end (event → consumer → DB),
/// not just that the HTTP call returned 200.
///
/// Requires the Docker stack: docker compose up -d --build
/// </summary>
[Trait("Category", "Integration")]
[Collection("Integration")]
public class NotificationConsumerTests
{
    private readonly IntegrationTestSetup _s;
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public NotificationConsumerTests(IntegrationTestSetup s) => _s = s;

    [Fact]
    public async Task Convention_Generated_Persists_A_Notification_For_The_Learner()
    {
        Assert.False(string.IsNullOrEmpty(_s.ConventionId),
            "Fixture did not receive a convention — is the Docker stack up and RabbitMQ reachable?");

        var resp = await PostAsAdminAsync($"/api/v1/conventions/{_s.ConventionId}/generer", new { });
        Assert.True(resp.IsSuccessStatusCode, $"generate failed: {resp.StatusCode} {await resp.Content.ReadAsStringAsync()}");

        var found = await WaitForNotificationAsync("ConventionGeneree", _s.LearnerUserId);
        Assert.True(found, "No ConventionGeneree notification was persisted for the learner.");
    }

    [Fact]
    public async Task Candidature_Rejected_Persists_A_Notification_For_The_Applicant()
    {
        // Self-contained: register a throwaway learner, submit, reject, and assert the applicant
        // (and only the applicant) got a notification row.
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var email = $"integ-reject-{ts}@stb.tn";

        var register = await PostAsync("/api/v1/auth/register", new
        {
            email,
            password = "TestPass123!",
            firstName = "Reject",
            lastName = "Learner"
        });
        Assert.True(register.IsSuccessStatusCode, $"register failed: {register.StatusCode}");
        var body = await register.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("token").GetString()!;
        var userId = body.GetProperty("userId").GetInt64();

        var submit = await PostAsTokenAsync(token, "/api/v1/candidatures", new
        {
            nom = "Reject",
            prenom = "Learner",
            email,
            departement = "IT",
            typeStage = "PFE",
            ecole = "ESPRIT",
            dateDebut = "2026-09-01",
            dateFin = "2026-12-01",
            motivation = "notification consumer test"
        });
        Assert.True(submit.IsSuccessStatusCode, $"submit failed: {submit.StatusCode}");
        var candidatureId = (await submit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;

        var reject = await PostAsAdminAsync($"/api/v1/candidatures/{candidatureId}/rejeter", new
        {
            motifRejet = "Motif de test suffisamment long"
        });
        Assert.True(reject.IsSuccessStatusCode, $"reject failed: {reject.StatusCode} {await reject.Content.ReadAsStringAsync()}");

        var found = await WaitForNotificationAsync("CandidatureRejetee", userId);
        Assert.True(found, "No CandidatureRejetee notification was persisted for the applicant.");
    }

    [Fact]
    public async Task Candidature_Accepted_Persists_A_Notification_For_The_Learner()
    {
        // The fixture already accepts the primary learner's candidature during setup, so the
        // consumer must have produced a notification by now.
        var found = await WaitForNotificationAsync("CandidatureAcceptee", _s.LearnerUserId, attempts: 3);
        Assert.True(found, "No CandidatureAcceptee notification was persisted for the learner.");
    }

    // ---- helpers ----

    /// <summary>
    /// Polls the admin notifications list until a notification of the given type addressed to the
    /// given recipient appears, or the attempts run out. Events are asynchronous (RabbitMQ), so a
    /// single immediate read would be flaky.
    /// </summary>
    private async Task<bool> WaitForNotificationAsync(string type, long destinataireId, int attempts = 20)
    {
        for (var i = 0; i < attempts; i++)
        {
            var resp = await GetAsAdminAsync("/api/v1/notifications?pageSize=100");
            if (resp.IsSuccessStatusCode)
            {
                var page = await resp.Content.ReadFromJsonAsync<JsonElement>();
                if (page.TryGetProperty("items", out var items))
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        var itemType = item.TryGetProperty("type", out var t) ? t.GetString() : null;
                        var recipient = item.TryGetProperty("destinataireId", out var d) && d.ValueKind == JsonValueKind.Number
                            ? d.GetInt64()
                            : (long?)null;

                        if (string.Equals(itemType, type, StringComparison.OrdinalIgnoreCase) && recipient == destinataireId)
                        {
                            return true;
                        }
                    }
                }
            }

            await Task.Delay(1000);
        }

        return false;
    }

    private async Task<HttpResponseMessage> PostAsync(string path, object body) =>
        await new HttpClient { BaseAddress = new Uri(IntegrationTestSetup.BaseUrl) }
            .PostAsJsonAsync(path, body);

    private async Task<HttpResponseMessage> PostAsAdminAsync(string path, object body)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body, options: _json) };
        req.Headers.Authorization = new("Bearer", _s.AdminToken);
        return await new HttpClient { BaseAddress = new Uri(IntegrationTestSetup.BaseUrl) }.SendAsync(req);
    }

    private async Task<HttpResponseMessage> PostAsTokenAsync(string token, string path, object body)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body, options: _json) };
        req.Headers.Authorization = new("Bearer", token);
        return await new HttpClient { BaseAddress = new Uri(IntegrationTestSetup.BaseUrl) }.SendAsync(req);
    }

    private async Task<HttpResponseMessage> GetAsAdminAsync(string path)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, path);
        req.Headers.Authorization = new("Bearer", _s.AdminToken);
        return await new HttpClient { BaseAddress = new Uri(IntegrationTestSetup.BaseUrl) }.SendAsync(req);
    }
}
