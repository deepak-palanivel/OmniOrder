using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OmniOrder.Api.Middleware;
using OmniOrder.Application.Interfaces;
using OmniOrder.Infrastructure.BackgroundWorkers;
using OmniOrder.Infrastructure.Channels;
using OmniOrder.Infrastructure.Data;
using OmniOrder.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database (EF Core + SQLite WAL) ──
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=omniorder.db";
builder.Services.AddDbContext<OmniOrderDbContext>(options =>
    options.UseSqlite(connectionString));

// ── Authentication (JWT Bearer) ──
var jwtKey = builder.Configuration["Jwt:Key"] ?? "OmniOrder-SuperSecret-JWT-Key-For-Development-2026-Must-Be-256-Bits!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "OmniOrder";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "OmniOrderClient";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        // Allow SignalR to use JWT from query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// ── Application Services ──
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IWebhookService, WebhookService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddSingleton<IConcurrencyTracker, ConcurrencyTracker>();

// ── Channels ──
builder.Services.AddSingleton<OrderAuditChannel>();

// ── Background Workers ──
builder.Services.AddHostedService<ExpiredReservationReaperService>();
builder.Services.AddHostedService<OutboxProcessorService>();

// ── SignalR ──
builder.Services.AddSignalR();

// ── Controllers ──
builder.Services.AddControllers(options =>
{
    // Enable request body buffering for the idempotency filter
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

// ── Exception Handler ──
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ── Swagger/OpenAPI ──
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OmniOrder API",
        Version = "v1",
        Description = "High-Concurrency Distributed Order Management, Inventory Locking & Payment Webhook Engine",
        Contact = new OpenApiContact { Name = "OmniOrder", Email = "dev@omniorder.dev" }
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. Get one from POST /api/v1/auth/demo-token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ── CORS (for Vite dev server) ──
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVite", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// ── Middleware Pipeline ──
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "OmniOrder API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowVite");

// Enable request body buffering for idempotency filter
app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<OrderHub>("/hubs/orders");

// ── Database Seeding ──
await DatabaseSeeder.SeedAsync(app.Services);

app.Run();

// Make the Program class accessible for integration tests
public partial class Program { }
