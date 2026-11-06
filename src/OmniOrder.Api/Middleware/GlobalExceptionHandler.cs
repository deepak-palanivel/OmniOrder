using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OmniOrder.Domain.Exceptions;

namespace OmniOrder.Api.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var problemDetails = exception switch
        {
            InsufficientStockException ex => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Insufficient Stock",
                Detail = ex.Message,
                Type = "https://omniorder.dev/errors/insufficient-stock",
                Extensions = { ["code"] = ex.Code, ["productId"] = ex.ProductId, ["requested"] = ex.RequestedQuantity, ["available"] = ex.AvailableStock }
            },
            ConcurrencyConflictException ex => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Concurrency Conflict",
                Detail = ex.Message,
                Type = "https://omniorder.dev/errors/concurrency-conflict",
                Extensions = { ["code"] = ex.Code }
            },
            OrderNotFoundException ex => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Order Not Found",
                Detail = ex.Message,
                Type = "https://omniorder.dev/errors/order-not-found",
                Extensions = { ["code"] = ex.Code }
            },
            DuplicateWebhookException ex => new ProblemDetails
            {
                Status = StatusCodes.Status200OK,
                Title = "Duplicate Webhook",
                Detail = ex.Message,
                Type = "https://omniorder.dev/errors/duplicate-webhook",
                Extensions = { ["code"] = ex.Code }
            },
            InvalidWebhookSignatureException ex => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Invalid Signature",
                Detail = ex.Message,
                Type = "https://omniorder.dev/errors/invalid-signature",
                Extensions = { ["code"] = ex.Code }
            },
            DomainException ex => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Domain Error",
                Detail = ex.Message,
                Type = "https://omniorder.dev/errors/domain-error",
                Extensions = { ["code"] = ex.Code }
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = exception.Message,
                Type = "https://omniorder.dev/errors/internal"
            }
        };

        httpContext.Response.StatusCode = problemDetails.Status ?? 500;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
