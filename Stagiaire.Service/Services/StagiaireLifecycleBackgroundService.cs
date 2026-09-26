using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stagiaire.Service.Data;
using Stagiaire.Service.Models;

namespace Stagiaire.Service.Services;

/// <summary>
/// Background worker that monitors internship dates and promotes stagiaire records:
/// - Acceptee -> EnCours when DateDebut <= today
/// - EnCours -> Termine when DateFin < today
/// </summary>
public class StagiaireLifecycleBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StagiaireLifecycleBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public StagiaireLifecycleBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<StagiaireLifecycleBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StagiaireLifecycleBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessTransitionsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error processing stagiaire lifecycle transitions.");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("StagiaireLifecycleBackgroundService stopped.");
    }

    public async Task ProcessTransitionsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // 1. Acceptee -> EnCours if internship has started
        var toStart = await db.Stagiaires
            .Where(s => s.Statut == StatutStagiaire.Acceptee && s.DateDebut <= today)
            .ToListAsync(cancellationToken);

        foreach (var stagiaire in toStart)
        {
            stagiaire.Statut = StatutStagiaire.EnCours;
            _logger.LogInformation("Promoted stagiaire {Id} ({Nom} {Prenom}) to EnCours",
                stagiaire.Id, stagiaire.Nom, stagiaire.Prenom);
        }

        // 2. EnCours -> Termine if internship has completed
        var toEnd = await db.Stagiaires
            .Where(s => s.Statut == StatutStagiaire.EnCours && s.DateFin < today)
            .ToListAsync(cancellationToken);

        foreach (var stagiaire in toEnd)
        {
            stagiaire.Statut = StatutStagiaire.Termine;
            _logger.LogInformation("Promoted stagiaire {Id} ({Nom} {Prenom}) to Termine",
                stagiaire.Id, stagiaire.Nom, stagiaire.Prenom);
        }

        if (toStart.Count > 0 || toEnd.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Lifecycle transitions completed: {Started} started (EnCours), {Ended} completed (Termine).",
                toStart.Count, toEnd.Count);
        }
    }
}
