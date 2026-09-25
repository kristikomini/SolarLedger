using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SolarLedger.Application.Settlement;
using SolarLedger.Domain.Settlement;
using SolarLedger.Infrastructure.Persistence;

namespace SolarLedger.Infrastructure.Settlement;

/// <summary>
/// Background worker that claims pending settlement runs and processes them. Safe to run
/// as many instances at once: the claim (see <see cref="SettlementRunClaimer"/>) hands
/// each worker a distinct run. Being a hosted service it is a singleton, but it needs a
/// scoped DbContext — so it takes an <see cref="IServiceScopeFactory"/> and opens a scope
/// per iteration rather than capturing the context (which would be a captive dependency).
/// </summary>
public sealed class SettlementRunProcessor : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SettlementRunProcessor> _logger;
    private readonly string _workerId = Environment.MachineName;

    public SettlementRunProcessor(
        IServiceScopeFactory scopeFactory, ILogger<SettlementRunProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Settlement worker {WorkerId} started.", _workerId);

        using var timer = new PeriodicTimer(PollInterval);
        do
        {
            try
            {
                // Drain: keep claiming while there is work, so a busy queue clears fast.
                while (await TryProcessOneAsync(stoppingToken)) { }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker {WorkerId} loop error; will retry.", _workerId);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task<bool> TryProcessOneAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SolarLedgerDbContext>();
        var claimer = new SettlementRunClaimer(db);

        var runId = await claimer.TryClaimAsync(_workerId, StaleAfter, ct);
        if (runId is null)
            return false;

        _logger.LogInformation("Worker {WorkerId} claimed run {RunId}.", _workerId, runId);

        var run = await db.SettlementRuns
            .Include(r => r.Community)
            .FirstAsync(r => r.Id == runId, ct);

        try
        {
            var tariff = run.Community!.IncentiveTariffEurPerMwh;
            var input = await SettlementQueries.LoadInputAsync(
                db, run.CommunityId, tariff, run.PeriodStartUtc, run.PeriodEndUtc, ct);

            var policy = DistributionPolicies.Resolve(run.PolicyName ?? run.Community.DistributionPolicy);
            var result = new SettlementCalculator(policy).Calculate(input);

            // Idempotent: clear any prior results for this run before writing (reclaim-safe).
            var oldMembers = await db.MemberSettlements
                .Where(m => m.SettlementRunId == run.Id).ToListAsync(ct);
            var oldHours = await db.HourlyShares
                .Where(h => h.SettlementRunId == run.Id).ToListAsync(ct);
            db.MemberSettlements.RemoveRange(oldMembers);
            db.HourlyShares.RemoveRange(oldHours);

            foreach (var m in result.Members)
                db.MemberSettlements.Add(new MemberSettlement
                {
                    SettlementRunId = run.Id,
                    MemberId = m.MemberId,
                    AttributedKwh = m.AttributedKwh,
                    IncentiveEur = m.IncentiveEur
                });

            foreach (var h in result.Hourly)
                db.HourlyShares.Add(new HourlyShare
                {
                    SettlementRunId = run.Id,
                    Hour = h.Hour,
                    InjectedKwh = h.InjectedKwh,
                    WithdrawnKwh = h.WithdrawnKwh,
                    SharedKwh = h.SharedKwh
                });

            run.TotalSharedKwh = result.TotalSharedKwh;
            run.TotalIncentiveEur = result.TotalIncentiveEur;
            run.PolicyName = result.PolicyName;
            run.Status = SettlementRunStatus.Completed;
            run.CompletedOnUtc = DateTime.UtcNow;
            run.ErrorMessage = null;

            await db.SaveChangesAsync(ct);
            _logger.LogInformation(
                "Worker {WorkerId} completed run {RunId}: {Shared} kWh, €{Eur}.",
                _workerId, run.Id, result.TotalSharedKwh, result.TotalIncentiveEur);
        }
        catch (Exception ex)
        {
            run.Status = SettlementRunStatus.Failed;
            run.ErrorMessage = ex.Message;
            await db.SaveChangesAsync(ct);
            _logger.LogError(ex, "Worker {WorkerId} failed run {RunId}.", _workerId, run.Id);
        }

        return true;
    }
}
