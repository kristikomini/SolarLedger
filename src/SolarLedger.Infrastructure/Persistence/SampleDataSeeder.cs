using Microsoft.EntityFrameworkCore;
using SolarLedger.Application.Metering;
using SolarLedger.Domain.Communities;
using SolarLedger.Domain.Members;
using SolarLedger.Domain.Pods;

namespace SolarLedger.Infrastructure.Persistence;

/// <summary>
/// Seeds one sample community (members, PODs) and a stretch of hourly readings, so the
/// settlement engine (Phase 2) has realistic data to work on. Idempotent: does nothing
/// if a community already exists.
/// </summary>
public static class SampleDataSeeder
{
    public record SeedResult(bool Seeded, long? CommunityId, int Members, int Pods, int Readings);

    public static async Task<SeedResult> SeedAsync(
        SolarLedgerDbContext db, int days = 7, int seed = 42, CancellationToken ct = default)
    {
        // NOTE: on Oracle < 23c a scalar AnyAsync() translates to
        // `CASE WHEN EXISTS(...) THEN True ELSE False` and fails (ORA-00904, no boolean
        // literals before 23c). Count-based checks emit COUNT(*) and are safe.
        if (await db.Communities.CountAsync(ct) > 0)
        {
            var existing = await db.Communities.FirstAsync(ct);
            return new SeedResult(false, existing.Id, 0, 0, 0);
        }

        var community = new Community
        {
            Name = "Motor Valley CER",
            PrimarySubstationCode = "CP_MO_01",
            IncentiveTariffEurPerMwh = 110m,
            DistributionPolicy = "AllToProducers"
        };

        var specs = new (string member, MemberRole role, string podCode, PodType type)[]
        {
            ("Impianto FV Capannone", MemberRole.Producer,  "IT001E00000001", PodType.Production),
            ("Famiglia Rossi",        MemberRole.Consumer,  "IT001E00000002", PodType.Consumption),
            ("Famiglia Bianchi",      MemberRole.Consumer,  "IT001E00000003", PodType.Consumption),
            ("Bar Centrale",          MemberRole.Consumer,  "IT001E00000004", PodType.Consumption),
            ("Officina Verdi",        MemberRole.Prosumer,  "IT001E00000005", PodType.Both),
            ("Scuola Comunale",       MemberRole.Prosumer,  "IT001E00000006", PodType.Both),
        };

        var podNumber = 1;
        foreach (var s in specs)
        {
            var member = new Member
            {
                Name = s.member,
                Role = s.role,
                PaymentReference = $"IT00X0000000000000000{podNumber:00}"
            };
            member.Pods.Add(new Pod { PodCode = s.podCode, Type = s.type });
            community.Members.Add(member);
            podNumber++;
        }

        db.Communities.Add(community);
        // Save once so members/PODs get their identities before generating readings.
        await db.SaveChangesAsync(ct);

        var startUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var pods = await db.Pods.Where(p => p.Member!.CommunityId == community.Id).ToListAsync(ct);

        var readingCount = 0;
        foreach (var pod in pods)
        {
            var readings = ReadingGenerator.Generate(pod, startUtc, days, seed);
            db.EnergyReadings.AddRange(readings);
            readingCount += readings.Count;
        }

        await db.SaveChangesAsync(ct);

        return new SeedResult(true, community.Id, specs.Length, pods.Count, readingCount);
    }
}
