using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OmniOrder.Application.Interfaces;
using OmniOrder.Domain.Entities;

namespace OmniOrder.Api.Middleware;

[AttributeUsage(AttributeTargets.Method)]
public class IdempotentAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue("X-Idempotency-Key", out var idempotencyKey) ||
            string.IsNullOrWhiteSpace(idempotencyKey))
        {
            context.Result = new BadRequestObjectResult(new ProblemDetails
            {
                Title = "Missing Idempotency Key",
                Detail = "The X-Idempotency-Key header is required for this request.",
                Status = StatusCodes.Status400BadRequest,
                Type = "https://omniorder.dev/errors/missing-idempotency-key"
            });
            return;
        }

        var idempotencyService = context.HttpContext.RequestServices.GetRequiredService<IIdempotencyService>();
        var key = idempotencyKey.ToString();

        var existingRecord = await idempotencyService.GetByKeyAsync(key);
        if (existingRecord != null)
        {
            context.HttpContext.Response.StatusCode = existingRecord.ResponseStatusCode;
            context.HttpContext.Response.ContentType = "application/json";
            await context.HttpContext.Response.WriteAsync(existingRecord.ResponseBody);
            return;
        }

        var executedContext = await next();

        if (executedContext.Result is ObjectResult objectResult)
        {
            var responseBody = System.Text.Json.JsonSerializer.Serialize(objectResult.Value, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
            var requestBody = string.Empty;

            context.HttpContext.Request.Body.Seek(0, SeekOrigin.Begin);
            using (var reader = new StreamReader(context.HttpContext.Request.Body, Encoding.UTF8, leaveOpen: true))
            {
                requestBody = await reader.ReadToEndAsync();
            }

            var requestHash = ComputeHash(requestBody);

            var record = new IdempotencyRecord
            {
                Id = Guid.NewGuid(),
                IdempotencyKey = key,
                RequestHash = requestHash,
                ResponseStatusCode = objectResult.StatusCode ?? 200,
                ResponseBody = responseBody,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await idempotencyService.SaveAsync(record);
            }
            catch (Exception)
            {
                // Idempotency record save failed — non-critical, continue
            }
        }
    }

    private static string ComputeHash(string input)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
