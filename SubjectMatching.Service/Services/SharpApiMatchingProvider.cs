using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SubjectMatching.Service.Services;

public class SharpApiMatchingProvider : IAiMatchingProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SharpApiMatchingProvider> _logger;

    public string ProviderName => "sharpapi";

    public SharpApiMatchingProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<SharpApiMatchingProvider> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ParsedCvData> ParseResumeAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["SHARP_API_KEY"] ?? _configuration["SharpApi:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("SharpAPI Key is missing in environment or configuration.");
            throw new InvalidOperationException("SharpAPI Key is not configured.");
        }

        var baseUrl = _configuration["SharpApi:BaseUrl"] ?? "https://sharpapi.com";
        _logger.LogInformation("Initiating resume parsing with SharpAPI for file {FileName}", fileName);

        // Prepare multipart form content
        using var multipartContent = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        multipartContent.Add(streamContent, "file", fileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/api/v1/hr/parse_resume")
        {
            Content = multipartContent
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("SharpAPI initial call failed with status {StatusCode}: {Error}", response.StatusCode, errorBody);
            throw new HttpRequestException($"SharpAPI failed: {response.StatusCode} - {errorBody}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);

        string? statusUrl = null;
        string? jobId = null;

        if (doc.RootElement.TryGetProperty("status_url", out var statusUrlProp))
        {
            statusUrl = statusUrlProp.GetString();
        }

        if (doc.RootElement.TryGetProperty("job_id", out var jobIdProp))
        {
            jobId = jobIdProp.GetString();
        }

        if (string.IsNullOrWhiteSpace(statusUrl) && !string.IsNullOrWhiteSpace(jobId))
        {
            statusUrl = $"{baseUrl.TrimEnd('/')}/api/v1/jobs/{jobId}";
        }

        if (string.IsNullOrWhiteSpace(statusUrl))
        {
            // Some versions return the parsed result directly
            if (doc.RootElement.TryGetProperty("data", out var dataProp))
            {
                return ExtractFromSharpApiResult(dataProp);
            }
            throw new InvalidOperationException("SharpAPI response contained neither a status URL nor direct data.");
        }

        // Poll for job completion with exponential / periodic backoff
        var maxAttempts = 15;
        var delayMs = 1500;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            await Task.Delay(delayMs, cancellationToken);

            using var pollRequest = new HttpRequestMessage(HttpMethod.Get, statusUrl);
            pollRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
            pollRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var pollResponse = await _httpClient.SendAsync(pollRequest, cancellationToken);
            if (!pollResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("SharpAPI polling attempt {Attempt} failed with {Status}", attempt, pollResponse.StatusCode);
                continue;
            }

            var pollContent = await pollResponse.Content.ReadAsStringAsync(cancellationToken);
            using var pollDoc = JsonDocument.Parse(pollContent);

            var root = pollDoc.RootElement;
            string? status = null;

            if (root.TryGetProperty("status", out var sProp))
            {
                status = sProp.GetString();
            }
            else if (root.TryGetProperty("data", out var dataElement) && dataElement.TryGetProperty("status", out var innerStatus))
            {
                status = innerStatus.GetString();
            }

            if (string.Equals(status, "completed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "success", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "done", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("SharpAPI job completed successfully in attempt {Attempt}", attempt);

                if (root.TryGetProperty("result", out var resultProp))
                {
                    return ExtractFromSharpApiResult(resultProp);
                }
                if (root.TryGetProperty("data", out var dProp) && dProp.TryGetProperty("result", out var innerResult))
                {
                    return ExtractFromSharpApiResult(innerResult);
                }
                return ExtractFromSharpApiResult(root);
            }

            if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("SharpAPI job failed on server side.");
                throw new InvalidOperationException("SharpAPI job status returned 'failed'.");
            }
        }

        throw new TimeoutException("SharpAPI job polling timed out after multiple attempts.");
    }

    private ParsedCvData ExtractFromSharpApiResult(JsonElement resultElement)
    {
        var data = new ParsedCvData();

        // Check candidate profile attributes
        if (resultElement.TryGetProperty("candidate_name", out var nameProp))
            data.CandidateName = nameProp.GetString();
        else if (resultElement.TryGetProperty("name", out var nProp))
            data.CandidateName = nProp.GetString();

        if (resultElement.TryGetProperty("email", out var emailProp))
            data.Email = emailProp.GetString();

        if (resultElement.TryGetProperty("phone", out var phoneProp))
            data.Phone = phoneProp.GetString();

        // Skills
        if (resultElement.TryGetProperty("skills", out var skillsProp) && skillsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in skillsProp.EnumerateArray())
            {
                var skill = item.ValueKind == JsonValueKind.String ? item.GetString() : item.ToString();
                if (!string.IsNullOrWhiteSpace(skill) && !data.Skills.Contains(skill, StringComparer.OrdinalIgnoreCase))
                {
                    data.Skills.Add(skill.Trim());
                }
            }
        }

        // Education
        if (resultElement.TryGetProperty("education", out var eduProp))
        {
            data.Education = eduProp.ToString();
        }

        // Experience
        if (resultElement.TryGetProperty("experience", out var expProp) ||
            resultElement.TryGetProperty("work_experience", out expProp))
        {
            data.Experience = expProp.ToString();
        }

        // Projects
        if (resultElement.TryGetProperty("projects", out var projProp) && projProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in projProp.EnumerateArray())
            {
                var project = item.ValueKind == JsonValueKind.String ? item.GetString() : item.ToString();
                if (!string.IsNullOrWhiteSpace(project))
                {
                    data.Projects.Add(project.Trim());
                }
            }
        }

        // Languages
        if (resultElement.TryGetProperty("languages", out var langProp) && langProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in langProp.EnumerateArray())
            {
                var lang = item.ValueKind == JsonValueKind.String ? item.GetString() : item.ToString();
                if (!string.IsNullOrWhiteSpace(lang))
                {
                    data.Languages.Add(lang.Trim());
                }
            }
        }

        return data;
    }
}
