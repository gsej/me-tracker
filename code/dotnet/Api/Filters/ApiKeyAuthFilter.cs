using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Api.Filters;

public class ApiKeyAuthFilter : IAuthorizationFilter
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private readonly string _apiKey;

    public ApiKeyAuthFilter(IConfiguration configuration)
    {
        _apiKey = configuration["ApiKey"] ?? throw new ArgumentNullException("ApiKey configuration is missing");
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedApiKey))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (!_apiKey.Equals(providedApiKey))
        {
            context.Result = new UnauthorizedResult();
        }
    }
}
