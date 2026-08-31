using DashboardApi.Data;
using DashboardApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Text.Json;

namespace DashboardApi.Services;

public class HealthCheckService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, (string Token, DateTime Expiry)> _cachedTokens = new();

    public HealthCheckService(IServiceScopeFactory scopeFactory, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        // Console.WriteLine($"BaseAddress: {_httpClient.BaseAddress}");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var services = await db.Services.ToListAsync(stoppingToken);

            foreach (var service in services)
            {
                var check = await PingServiceAsync(service);
                db.StatusChecks.Add(check);
            }

            await db.SaveChangesAsync(stoppingToken);

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task<string?> GetAutheliaTokenAsync(string audience)
    {
        if (_cachedTokens.TryGetValue(audience, out var cached) && DateTime.UtcNow < cached.Expiry)
            return cached.Token;

        var clientSecret = _configuration["Authelia:ClientSecret"];
        if (string.IsNullOrEmpty(clientSecret))
            return null;

        var request = new HttpRequestMessage(HttpMethod.Post, "your-domain.example.com/authelia/api/oidc/token");
        var authBytes = System.Text.Encoding.UTF8.GetBytes($"healthchecker:{clientSecret}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["scope"] = "authelia.bearer.authz",
            ["audience"] = audience
        });

        var response = await _httpClient.SendAsync(request);
        // Console.WriteLine($"[TOKEN] audience={audience} status={response.StatusCode}");
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = json.GetProperty("access_token").GetString();
        if (token == null) return null;

        var expiry = DateTime.UtcNow.AddSeconds(json.GetProperty("expires_in").GetInt32() - 60);
        _cachedTokens[audience] = (token, expiry);
        return token;
    }

    private async Task<StatusCheck> PingServiceAsync(Service service)
    {
        var startTime = DateTime.UtcNow;
        bool isUp;
        int responseTimeMs;

        try
        {
            var checkUrl = string.IsNullOrEmpty(service.HealthCheckUrl) ? service.Url : service.HealthCheckUrl;
            var request = new HttpRequestMessage(HttpMethod.Get, checkUrl)
            {
                Version = System.Net.HttpVersion.Version11,
                VersionPolicy = System.Net.Http.HttpVersionPolicy.RequestVersionExact
            }
            ;

            if (service.RequiresAuth)
            {
                var token = await GetAutheliaTokenAsync(checkUrl);
                if (token != null)
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            // Console.WriteLine($"[DEBUG] {service.Name} -> Status: {(int)response.StatusCode} {response.StatusCode}, Body: '{body}'");
            isUp = response.IsSuccessStatusCode || (int)response.StatusCode is >= 300 and < 400;
        }
        catch (Exception ex)
        {
            // Console.WriteLine($"[DEBUG] {service.Name} EXCEPTION: {ex.GetType().FullName} - {ex.Message}");
            // if (ex.InnerException != null)
            //     Console.WriteLine($"[DEBUG] {service.Name} INNER: {ex.InnerException.GetType().FullName} - {ex.InnerException.Message}");
            isUp = false;
        }

        responseTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

        return new StatusCheck
        {
            ServiceId = service.Id,
            CheckedAt = DateTime.UtcNow,
            IsUp = isUp,
            ResponseTimeMs = responseTimeMs
        };
    }
}