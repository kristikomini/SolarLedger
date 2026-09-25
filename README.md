# SolarLedger

A settlement engine for a **Renewable Energy Community (CER)**: it ingests hourly meter
readings per connection point (POD), computes the **shared energy** each hour as
`min(Σ injected, Σ withdrawn)` across the community, applies the incentive tariff, and
splits it among members — the mechanism behind Italian *autoconsumo condiviso*.

Built on **.NET 8, C#, Oracle and Docker**. Portfolio project — deliberately scoped:
faithful settlement logic, not GSE-certified compliance. See [`PLAN.md`](PLAN.md) for
the full design and the simplifications taken.

## Status

**Phase 0 — skeleton (done).** Layered .NET 8 solution, Oracle XE in Docker, EF Core on
the Oracle provider, migrations, and a `/health` endpoint that pings the database.
`docker compose up` brings up Oracle, waits for it to be healthy, and the API
auto-applies migrations on startup.

**Phase 1 — data model + generator (done).** Entities `Community`, `Member`, `Pod`,
`EnergyReading`; a deterministic hourly-reading generator (solar bell curve for
production, morning/evening peaks for consumption); a sample-data seeder; read/seed
endpoints. Seeding a 7-day sample community produces 1,008 hourly readings across 6
PODs, persisted in Oracle.

**Phase 2 — settlement engine (done).** Pure calculator: shared energy per hour =
`min(Σ injected, Σ withdrawn)`, incentive = shared × tariff, split by a swappable
`IDistributionPolicy` (`AllToProducers`, `ProportionalSplit`). Money is split with a
largest-remainder method that reconciles to the total to the cent. A read-only
`/settlement/preview` endpoint runs it over stored readings. 17 unit tests cover the
edge cases (simultaneity limits, no-overlap, producer-only payout, money reconciliation).

**Phases 3 + 4 — persisted, idempotent, multi-worker runs (done).** A `SettlementRun`
(with `MemberSettlement` and `HourlyShare` results) is enqueued as `Pending`, unique per
`(community, period)` so re-enqueueing is idempotent. Background **workers** claim runs
with Oracle `SELECT ... FOR UPDATE SKIP LOCKED` — so any number of worker instances is
safe: each takes a different run, none is processed twice, and a run stuck `InProgress`
past a timeout (its worker died) is reclaimed. The API is the control plane (enqueue +
report, applies migrations); the `worker` service scales independently. Verified live
with two workers splitting a 7-run queue.

Architecture note: the worker is a hosted-service singleton but needs a scoped
`DbContext`, so it uses `IServiceScopeFactory` (a scope per iteration) rather than
capturing the context — avoiding a captive dependency.

**Phase 5 — dashboard + report (done).** A small React dashboard (served from
`wwwroot` at `/`, no build step) shows a community's runs, per-member payouts, and an
hourly shared-energy chart, with a **GSE-style CSV export** per run. The plan's core
(Phases 0–5) is complete.

## Try it (after `docker compose up -d`)

```bash
curl -X POST "http://localhost:5080/dev/seed?days=7"   # seed sample community (idempotent)
curl http://localhost:5080/communities                 # list
curl http://localhost:5080/communities/1               # members + PODs
curl http://localhost:5080/communities/1/energy-summary # totals over the metered data
curl "http://localhost:5080/communities/1/settlement/preview"                        # run the engine
curl "http://localhost:5080/communities/1/settlement/preview?policy=ProducersConsumers50" # 50/50 split
```

### Multi-worker settlement runs

```bash
docker compose up -d --build --scale worker=2                # API + 2 workers
curl -X POST "http://localhost:5080/communities/1/settlement/runs/enqueue-daily?start=2026-09-01&days=7"
curl http://localhost:5080/communities/1/settlement/runs      # watch status + claimedBy per run
curl http://localhost:5080/settlement/runs/1                  # one run's persisted payouts
```

The `claimedBy` column shows the two worker hostnames splitting the queue — proof the
`SKIP LOCKED` claim coordinates multiple instances.

### Dashboard

Open **http://localhost:5080/** for the React dashboard (runs, payouts, hourly chart,
CSV download). Report export: `GET /settlement/runs/{id}/report.csv`.

## Gotcha found & handled: Oracle < 23c has no boolean literals

A scalar EF `AnyAsync()` translates to `CASE WHEN EXISTS(...) THEN True ELSE False` —
Oracle 21c rejects `True`/`False` (`ORA-00904`), since SQL booleans arrived in 23c.
Existence checks here use `CountAsync() > 0` (emits `COUNT(*)`) instead. Keep this in
mind for the settlement idempotency checks in Phase 3.

## Run it

```bash
docker compose up -d          # starts Oracle + API; API migrates once Oracle is healthy
curl http://localhost:5080/health   # -> Healthy (200) once the DB connection is up
curl http://localhost:5080/         # service info
docker compose down           # stop (keeps the DB volume)
docker compose down -v        # stop and wipe the database
```

First boot pulls the Oracle image (~2 GB) and initialises the database, so give it a
minute the first time.

## Layout

| Project | Role |
|---|---|
| `SolarLedger.Domain` | Entities, no framework dependencies. Phase 0: `Community`. |
| `SolarLedger.Application` | Use cases and the `ISolarLedgerDbContext` abstraction. |
| `SolarLedger.Infrastructure` | EF Core + Oracle: `DbContext`, configurations, migrations, DI. |
| `SolarLedger.Api` | ASP.NET Core host, `/health`, startup migration. |

## Notes

- **.NET 8** on purpose (matches the target stack); the Docker build uses the `sdk:8.0`
  image, so it is reproducible regardless of the host SDK.
- Oracle image: `gvenzl/oracle-xe:21-slim` (community image, no Oracle-registry login).
  Schema `SOLAR` / PDB `XEPDB1`. Credentials in `docker-compose.yml` are local-dev only.
- Regenerate the migration (needs the .NET 8 runtime, or roll-forward on a newer one):
  ```bash
  dotnet ef migrations add <Name> \
    -p src/SolarLedger.Infrastructure -s src/SolarLedger.Api -o Persistence/Migrations
  ```
