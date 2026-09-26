using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.AspNetCore;
using Notification.Service.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Steeltoe.Discovery.Eureka;
using Smartek.Common.Extensions;
using Notification.Service.Data;
using Notification.Service.Hubs;
using Notification.Service.Validation;
using MassTransit;
using RabbitMQ.Client;
using Stagiaire.Contracts.Events;
using Notification.Service.Consumers;
using Notification.Service.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddSmartekApiConventions();
builder.Services.AddSmartekSwagger(
    "Notification Service API",
    "Consomme les événements RabbitMQ, envoie les notifications par email, et diffuse les notifications en temps réel via SignalR.");

// SignalR — real-time push notifications (WebSocket, JWT authenticated).
builder.Services.AddSignalR();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3)));

builder.Services.AddSmartekHealthChecks<AppDbContext>();

builder.Services.AddEurekaDiscoveryClient();
builder.Services.AddJwtAuthentication(builder.Configuration);

// Allow SignalR clients to pass the JWT via query string (WebSocket connections cannot send headers).
builder.Services.PostConfigure<JwtBearerOptions>(
    JwtBearerDefaults.AuthenticationScheme,
    options =>
    {
        var existingOnMessageReceived = options.Events?.OnMessageReceived;
        options.Events ??= new JwtBearerEvents();
        options.Events.OnMessageReceived = async context =>
        {
            if (existingOnMessageReceived != null)
                await existingOnMessageReceived(context);

            var accessToken = context.Request.Query["access_token"].FirstOrDefault();
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub"))
            {
                context.Token = accessToken;
            }
        };
    });

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<NotificationCreateDtoValidator>();

// HttpClient for internal service-to-service calls (admin roster lookup in the
// EvaluationSubmittedAdminConsumer). Never routed through the gateway.
builder.Services.AddHttpClient("auth");

// Email service — configured via Smtp__* env vars (Mailhog in local dev, real SMTP in prod).
builder.Services.Configure<SmtpEmailOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddSingleton<IEmailService, SmtpEmailService>();

// Add MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    // Register consumers
    x.AddConsumer<ConventionGeneratedConsumer>();
    x.AddConsumer<CandidatureAcceptedConsumer>();
    x.AddConsumer<CandidatureSubmittedConsumer>();
    x.AddConsumer<EvaluationSubmittedConsumer>();
    x.AddConsumer<EvaluationSubmittedAdminConsumer>();
    x.AddConsumer<EvaluationValidatedConsumer>();
    x.AddConsumer<CandidatureRejectedConsumer>();
    x.AddConsumer<EncadrantCreatedConsumer>();
    x.AddConsumer<SubjectProposedConsumer>();
    x.AddConsumer<SubjectAcceptedConsumer>();
    x.AddConsumer<SubjectChangeRequestedConsumer>();
    x.AddConsumer<SubjectChangeReviewedConsumer>();
    x.AddConsumer<PasswordResetRequestedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });

        // Retry transient failures (SMTP timeouts in particular) with exponential back-off. After
        // the limit, the message lands in the transport dead-letter queue "<queue>_error" — a real
        // DLQ, inspectable in the RabbitMQ UI — instead of being dropped.
        cfg.UseMessageRetry(r => r.Exponential(
            retryLimit: 5,
            minInterval: TimeSpan.FromSeconds(1),
            maxInterval: TimeSpan.FromSeconds(30),
            intervalDelta: TimeSpan.FromSeconds(5)));

        // Raw-JSON interop: auth-service (Java / Spring AMQP) publishes EncadrantCreated and
        // PasswordResetRequested as plain JSON bodies on exchange "smartek.events" — no MassTransit envelope.
        cfg.UseRawJsonDeserializer();

        // Configure message endpoints
        cfg.ReceiveEndpoint("encadrant-created-queue", e =>
        {
            e.ConfigureConsumeTopology = false;
            e.Bind("smartek.events", s =>
            {
                s.RoutingKey = "encadrant.created";
                s.ExchangeType = ExchangeType.Topic;
            });
            e.ConfigureConsumer<EncadrantCreatedConsumer>(context);
        });

        cfg.ReceiveEndpoint("password-reset-requested-queue", e =>
        {
            e.ConfigureConsumeTopology = false;
            e.Bind("smartek.events", s =>
            {
                s.RoutingKey = "password.reset.requested";
                s.ExchangeType = ExchangeType.Topic;
            });
            e.ConfigureConsumer<PasswordResetRequestedConsumer>(context);
        });

        cfg.ReceiveEndpoint("subject-matching-queue", e =>
        {
            e.ConfigureConsumer<SubjectProposedConsumer>(context);
            e.ConfigureConsumer<SubjectAcceptedConsumer>(context);
            e.ConfigureConsumer<SubjectChangeRequestedConsumer>(context);
            e.ConfigureConsumer<SubjectChangeReviewedConsumer>(context);
        });

        cfg.ReceiveEndpoint("convention-generated-queue", e =>
        {
            e.ConfigureConsumer<ConventionGeneratedConsumer>(context);
        });

        cfg.ReceiveEndpoint("candidature-submitted-queue", e =>
        {
            e.ConfigureConsumer<CandidatureSubmittedConsumer>(context);
        });

        cfg.ReceiveEndpoint("candidature-accepted-queue", e =>
        {
            e.ConfigureConsumer<CandidatureAcceptedConsumer>(context);
        });

        cfg.ReceiveEndpoint("evaluation-submitted-queue", e =>
        {
            e.ConfigureConsumer<EvaluationSubmittedConsumer>(context);
            e.ConfigureConsumer<EvaluationSubmittedAdminConsumer>(context);
        });

        cfg.ReceiveEndpoint("evaluation-validated-queue", e =>
        {
            e.ConfigureConsumer<EvaluationValidatedConsumer>(context);
        });

        cfg.ReceiveEndpoint("candidature-rejected-queue", e =>
        {
            e.ConfigureConsumer<CandidatureRejectedConsumer>(context);
        });
    });
});

var app = builder.Build();

// Creates the database if absent and applies pending migrations before serving traffic.
await app.MigrateDatabaseAsync<AppDbContext>();

app.UseSmartekTracing();
app.UseSmartekExceptionHandling();

// Prometheus metrics — /metrics endpoint for scraping, HTTP request counters/histograms.
app.UseSmartekMetrics();

app.UseSmartekHealthChecks();

app.UseSmartekSwagger("Notification Service API");

// See Stagiaire.Service/Program.cs — TLS terminates at the gateway, so no HTTPS redirect here.
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hub/notifications");
app.MapSmartekFallback();

app.Run();

public partial class Program;
