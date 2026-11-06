using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OmniOrder.Application.DTOs;
using OmniOrder.Infrastructure.Data;

namespace OmniOrder.IntegrationTests;

public class OmniOrderIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public OmniOrderIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<OmniOrderDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                // Use a unique in-memory SQLite database per test run
                var testDbName = $"Data Source=test_{Guid.NewGuid():N}.db";
                services.AddDbContext<OmniOrderDbContext>(options =>
                    options.UseSqlite(testDbName));
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task FlashSale_50ConcurrentBuyers_Exactly5Succeed_45Fail()
    {
        // Act
        var response = await _client.PostAsync(
            "/api/v1/simulator/flash-sale?buyers=50&stock=5", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<FlashSaleResult>();
        Assert.NotNull(result);
        Assert.Equal(50, result.TotalAttempts);
        Assert.Equal(5, result.SuccessfulOrders);
        Assert.Equal(45, result.FailedAttempts);

        // Verify no negative stock
        var productsResponse = await _client.GetAsync("/api/v1/products");
        var products = await productsResponse.Content.ReadFromJsonAsync<List<ProductResponse>>();
        Assert.NotNull(products);
        foreach (var product in products)
        {
            Assert.True(product.AvailableStock >= 0, $"Product {product.Name} has negative stock: {product.AvailableStock}");
        }
    }

    [Fact]
    public async Task CreateOrder_WithIdempotencyKey_DuplicateReturnsOriginalResponse()
    {
        // Arrange
        var productsResponse = await _client.GetAsync("/api/v1/products");
        var products = await productsResponse.Content.ReadFromJsonAsync<List<ProductResponse>>();
        Assert.NotNull(products);
        var product = products.First(p => p.AvailableStock > 2);

        var request = new CreateOrderRequest(
            Guid.NewGuid(),
            new List<OrderItemRequest> { new(product.Id, 1) }
        );

        var idempotencyKey = Guid.NewGuid().ToString();

        // Act — first request
        var firstMessage = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders")
        {
            Content = JsonContent.Create(request)
        };
        firstMessage.Headers.Add("X-Idempotency-Key", idempotencyKey);
        var firstResponse = await _client.SendAsync(firstMessage);

        // Act — duplicate request with same key
        var secondMessage = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders")
        {
            Content = JsonContent.Create(request)
        };
        secondMessage.Headers.Add("X-Idempotency-Key", idempotencyKey);
        var secondResponse = await _client.SendAsync(secondMessage);

        // Assert
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var firstBody = await firstResponse.Content.ReadAsStringAsync();
        var secondBody = await secondResponse.Content.ReadAsStringAsync();
        Assert.Equal(firstBody, secondBody);
    }

    [Fact]
    public async Task WebhookPayment_DuplicateEventId_ProcessedOnlyOnce()
    {
        // Arrange — create an order first
        var productsResponse = await _client.GetAsync("/api/v1/products");
        var products = await productsResponse.Content.ReadFromJsonAsync<List<ProductResponse>>();
        Assert.NotNull(products);
        var product = products.First(p => p.AvailableStock > 2);

        var createRequest = new CreateOrderRequest(
            Guid.NewGuid(),
            new List<OrderItemRequest> { new(product.Id, 1) }
        );

        var createMessage = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders")
        {
            Content = JsonContent.Create(createRequest)
        };
        createMessage.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());
        var createResponse = await _client.SendAsync(createMessage);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var orderResponse = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(orderResponse);

        // Arrange — generate signed webhook payload
        var webhookRequest = new WebhookPaymentRequest(
            EventId: Guid.NewGuid().ToString(),
            EventType: "PAYMENT_CAPTURED",
            OrderId: orderResponse.Id,
            Amount: orderResponse.TotalAmount,
            Currency: "USD",
            Timestamp: DateTime.UtcNow
        );

        var signResponse = await _client.PostAsJsonAsync("/api/v1/webhooks/generate-signed-payload", webhookRequest);
        var signResult = await signResponse.Content.ReadFromJsonAsync<JsonElement>();
        var signature = signResult.GetProperty("signature").GetString()!;

        // Act — send the webhook 5 times (simulating network retries)
        var results = new List<HttpResponseMessage>();
        for (int i = 0; i < 5; i++)
        {
            var webhookMessage = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/payment")
            {
                Content = JsonContent.Create(webhookRequest)
            };
            webhookMessage.Headers.Add("X-Webhook-Signature", signature);
            webhookMessage.Headers.Add("X-Idempotency-Key", $"webhook-{webhookRequest.EventId}-{i}");
            results.Add(await _client.SendAsync(webhookMessage));
        }

        // Assert — first succeeds, rest are duplicates
        Assert.Equal(HttpStatusCode.OK, results[0].StatusCode);

        // Verify order is Paid
        var orderCheck = await _client.GetAsync($"/api/v1/orders/{orderResponse.Id}");
        var updatedOrder = await orderCheck.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(updatedOrder);
        Assert.Equal("Paid", updatedOrder.Status);
    }

    [Fact]
    public async Task DashboardMetrics_ReturnsValidMetrics()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/analytics/dashboard-metrics");

        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"Failed with: {response.StatusCode} - {content}");

        var metrics = await response.Content.ReadFromJsonAsync<DashboardMetrics>();
        Assert.NotNull(metrics);
        Assert.True(metrics.TotalOrders >= 0);
        Assert.True(metrics.DapperQueryTimeMs >= 0);
        Assert.True(metrics.EfCoreQueryTimeMs >= 0);
    }

    [Fact]
    public async Task DemoToken_ReturnsValidJwtToken()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/demo-token",
            new { Username = "testadmin", Role = "Admin" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(token);
        Assert.NotEmpty(token.Token);
        Assert.Equal("Admin", token.Role);
    }
}