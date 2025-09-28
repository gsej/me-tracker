using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Api.Filters;

public record ApiKey(string UserId, string Key);

public class ApiKeyAuthFilter : IAuthorizationFilter
{
    public const string UserIdKeyname = "UserId";
    private const string ApiKeyHeaderName = "X-API-Key";
    private readonly IEnumerable<ApiKey> _apiKeys;

    public ApiKeyAuthFilter(IConfiguration configuration)
    {
        var apiKeysJson = configuration["ApiKeys"] ?? throw new ArgumentNullException("ApiKey configuration is missing");
        
        _apiKeys = JsonSerializer.Deserialize<IEnumerable<ApiKey>>(apiKeysJson) 
                   ?? throw new ArgumentNullException("ApiKey configuration is invalid");
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedApiKey))
        {
            context.Result = new UnauthorizedResult();
            return;
        }
        
        var matchingApiKey = _apiKeys.SingleOrDefault(apiKey => apiKey.Key == providedApiKey);
        if (matchingApiKey == null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }
        
        context.HttpContext.Items[UserIdKeyname] = matchingApiKey.UserId;
    }
}
