using Elsa.Identity.Models;
using System.Security.Cryptography;

namespace WorkflowHost.ApiKeyManager;

public interface IApiKeyStore
{
    Task<ApiKey?> GetApiKeyAsync(string key);
    Task<ApiKey> CreateApiKeyAsync(string owner, List<string> roles, DateTime? expires = null);
    Task RevokeApiKeyAsync(string key);
}

public class InMemoryApiKeyStore : IApiKeyStore
{
    private readonly Dictionary<string, ApiKey> _apiKeys = new();

    public Task<ApiKey?> GetApiKeyAsync(string key)
    {
        _apiKeys.TryGetValue(key, out var apiKey);
        return Task.FromResult(apiKey);
    }

    public Task<ApiKey> CreateApiKeyAsync(string owner, List<string> roles, DateTime? expires = null)
    {
        var apiKey = new ApiKey
        {
            Key = GenerateApiKey(),
            Owner = owner,
            Created = DateTime.UtcNow,
            Expires = expires,
            Roles = roles,
            IsActive = true
        };

        _apiKeys[apiKey.Key] = apiKey;
        return Task.FromResult(apiKey);
    }

    public Task RevokeApiKeyAsync(string key)
    {
        if (_apiKeys.TryGetValue(key, out var apiKey))
        {
            apiKey.IsActive = false;
        }
        return Task.CompletedTask;
    }

    private static string GenerateApiKey()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}