using Microsoft.EntityFrameworkCore;
using SolarLedger.Api.Endpoints;
using SolarLedger.Infrastructure;
using SolarLedger.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

// /health pings the Oracle connection through the DbContext.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<SolarLedgerDbContext>("oracle");

var app = builder.Build();

// Only the control-plane instance migrates; workers skip it and just poll (ApplyMigrations=false),
// so concurrent workers never race on the migration history.
if (builder.Configuration.GetValue("ApplyMigrations", true))
    await ApplyMigrationsAsync(app);

// Serve the dashboard (wwwroot/index.html) at "/".
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/info", () => Results.Ok(new
{
    service = "SolarLedger.Api",
    status = "up",
    docs = "/health"
}));

app.MapHealthChecks("/health");
app.MapDataEndpoints();
app.MapSettlementEndpoints();
app.MapSettlementRunEndpoints();

app.Run();

static async Task ApplyMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<SolarLedgerDbContext>();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    const int maxAttempts = 20;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            logger.LogInformation("Migrations applied (attempt {Attempt}).", attempt);
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(
                "Database not ready (attempt {Attempt}/{Max}): {Message}. Retrying in 5s...",
                attempt, maxAttempts, ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}

// Exposed so integration tests (Phase 3) can reference the entry point.
public partial class Program;
