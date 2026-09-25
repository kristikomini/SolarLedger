using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SolarLedger.Application.Abstractions;
using SolarLedger.Infrastructure.Persistence;
using SolarLedger.Infrastructure.Settlement;

namespace SolarLedger.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' is not configured.");

        // Scoped by default via AddDbContext — the DbContext is a per-request unit of work.
        services.AddDbContext<SolarLedgerDbContext>(options =>
            options.UseOracle(connectionString));

        services.AddScoped<ISolarLedgerDbContext>(
            sp => sp.GetRequiredService<SolarLedgerDbContext>());

        // The settlement worker runs where enabled (workers on, API off). Many instances
        // are safe — runs are claimed with FOR UPDATE SKIP LOCKED.
        if (configuration.GetValue("Worker:Enabled", true))
            services.AddHostedService<SettlementRunProcessor>();

        return services;
    }
}
