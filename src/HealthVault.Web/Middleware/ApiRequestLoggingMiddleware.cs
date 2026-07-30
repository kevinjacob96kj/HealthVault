using HealthVault.Application.Requests;
using MediatR;

namespace HealthVault.Web.Middleware;

public class ApiRequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiRequestLoggingMiddleware> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public ApiRequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<ApiRequestLoggingMiddleware> logger,
        IServiceScopeFactory scopeFactory)
    {
        _next = next;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var isInsertOrUpdate =
            HttpMethods.IsPost(context.Request.Method) ||
            HttpMethods.IsPut(context.Request.Method) ||
            HttpMethods.IsPatch(context.Request.Method);

        var isAuthEndpoint = context.Request.Path.StartsWithSegments("/api/auth");

        if (!context.Request.Path.StartsWithSegments("/api") ||
            !isInsertOrUpdate ||
            isAuthEndpoint)
        {
            await _next(context);
            return;
        }

        var requestApi = $"{context.Request.Method} {context.Request.Path}";
        var requestDateTime = DateTime.UtcNow;
        var requestedBy = context.User.Identity?.Name
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "Anonymous";
        var wasSuccessful = false;

        try
        {
            await _next(context);
            wasSuccessful = context.Response.StatusCode is >= 200 and < 400;
        }
        finally
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();

                await sender.Send(
                    new LogApiRequestCommand
                    {
                        RequestApi = requestApi,
                        RequestDateTime = requestDateTime,
                        WasSuccessful = wasSuccessful,
                        RequestedBy = requestedBy
                    },
                    CancellationToken.None);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unable to audit API request {RequestApi}.",
                    requestApi);
            }
        }
    }
}
