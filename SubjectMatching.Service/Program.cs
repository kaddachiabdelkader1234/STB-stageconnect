using System.Text.Json.Serialization;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Smartek.Common.Extensions;
using Steeltoe.Discovery.Eureka;
using SubjectMatching.Service.Authentication;
using SubjectMatching.Service.Data;
using SubjectMatching.Service.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddSmartekApiConventions();
builder.Services.AddSmartekSwagger(
    "Subject Matching Service API",
    "Allocation et recommandation assistée par IA de sujets de stage STB.");

builder.Services.AddDbContext<SubjectMatchingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3)));

builder.Services.AddSmartekHealthChecks<SubjectMatchingDbContext>();

// AI Matching & Engine Services
builder.Services.AddHttpClient<SharpApiMatchingProvider>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddSingleton<IAiMatchingProvider, SharpApiMatchingProvider>();
builder.Services.AddSingleton<IAiMatchingProvider, MockMatchingProvider>();
builder.Services.AddSingleton<MatchingEngine>();

builder.Services.AddEurekaDiscoveryClient();
builder.Services.AddJwtAuthentication(builder.Configuration);

// Add MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });

        cfg.UseMessageRetry(r => r.Exponential(
            retryLimit: 5,
            minInterval: TimeSpan.FromSeconds(1),
            maxInterval: TimeSpan.FromSeconds(30),
            intervalDelta: TimeSpan.FromSeconds(5)));

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// Creates database and applies migrations before serving traffic
await app.MigrateDatabaseAsync<SubjectMatchingDbContext>();

app.UseSmartekTracing();
app.UseSmartekExceptionHandling();
app.UseSmartekMetrics();
app.UseSmartekHealthChecks();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Subject Matching Service API v1");
    });
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
