using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace WorkflowHost.ApiKeyManager;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private readonly IApiKeyStore _apiKeyStore;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyStore apiKeyStore)
        : base(options, logger, encoder)
    {
        _apiKeyStore = apiKeyStore;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeaderValues))
        {
            return AuthenticateResult.NoResult();
        }

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            return AuthenticateResult.NoResult();
        }

        var apiKey = await _apiKeyStore.GetApiKeyAsync(providedApiKey);

        if (apiKey == null)
        {
            return AuthenticateResult.Fail("Invalid API Key");
        }

        if (!apiKey.IsActive)
        {
            return AuthenticateResult.Fail("API Key is not active");
        }

        if (apiKey.Expires.HasValue && apiKey.Expires.Value < DateTime.UtcNow)
        {
            return AuthenticateResult.Fail("API Key has expired");
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, apiKey.Owner),
            new Claim("ApiKey", providedApiKey)
        };

        claims.AddRange(apiKey.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
