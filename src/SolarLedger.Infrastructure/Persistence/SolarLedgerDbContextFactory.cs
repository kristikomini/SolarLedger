using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SolarLedger.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so `dotnet ef migrations add` can build the model without a
/// running database or the API host. The connection string here is only used to
/// construct the context at design time; migrations are provider-shaped, not executed.
/// </summary>
public class SolarLedgerDbContextFactory : IDesignTimeDbContextFactory<SolarLedgerDbContext>
{
    public SolarLedgerDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "User Id=SOLAR;Password=Solar_2026;Data Source=localhost:1521/XEPDB1;";

        var options = new DbContextOptionsBuilder<SolarLedgerDbContext>()
            .UseOracle(connectionString)
            .Options;

        return new SolarLedgerDbContext(options);
    }
}
