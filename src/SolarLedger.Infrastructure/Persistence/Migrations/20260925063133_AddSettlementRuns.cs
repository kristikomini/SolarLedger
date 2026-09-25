using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SolarLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSettlementRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SETTLEMENT_RUNS",
                schema: "SOLAR",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    COMMUNITY_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    PERIOD_START_UTC = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    PERIOD_END_UTC = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    STATUS = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    POLICY_NAME = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: true),
                    TOTAL_SHARED_KWH = table.Column<decimal>(type: "DECIMAL(18,3)", precision: 18, scale: 3, nullable: false),
                    TOTAL_INCENTIVE_EUR = table.Column<decimal>(type: "DECIMAL(18,2)", precision: 18, scale: 2, nullable: false),
                    CLAIMED_BY = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    CLAIMED_AT_UTC = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    CREATED_ON_UTC = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    COMPLETED_ON_UTC = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    ERROR_MESSAGE = table.Column<string>(type: "NVARCHAR2(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SETTLEMENT_RUNS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SETTLEMENT_RUNS_COMMUNITIES_COMMUNITY_ID",
                        column: x => x.COMMUNITY_ID,
                        principalSchema: "SOLAR",
                        principalTable: "COMMUNITIES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HOURLY_SHARES",
                schema: "SOLAR",
                columns: table => new
                {
                    Id = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    SettlementRunId = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    Hour = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    InjectedKwh = table.Column<decimal>(type: "DECIMAL(18,3)", precision: 18, scale: 3, nullable: false),
                    WithdrawnKwh = table.Column<decimal>(type: "DECIMAL(18,3)", precision: 18, scale: 3, nullable: false),
                    SharedKwh = table.Column<decimal>(type: "DECIMAL(18,3)", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HOURLY_SHARES", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HOURLY_SHARES_SETTLEMENT_RUNS_SettlementRunId",
                        column: x => x.SettlementRunId,
                        principalSchema: "SOLAR",
                        principalTable: "SETTLEMENT_RUNS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MEMBER_SETTLEMENTS",
                schema: "SOLAR",
                columns: table => new
                {
                    Id = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    SettlementRunId = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    MemberId = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    AttributedKwh = table.Column<decimal>(type: "DECIMAL(18,3)", precision: 18, scale: 3, nullable: false),
                    IncentiveEur = table.Column<decimal>(type: "DECIMAL(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MEMBER_SETTLEMENTS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MEMBER_SETTLEMENTS_SETTLEMENT_RUNS_SettlementRunId",
                        column: x => x.SettlementRunId,
                        principalSchema: "SOLAR",
                        principalTable: "SETTLEMENT_RUNS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HOURLY_SHARES_SettlementRunId_Hour",
                schema: "SOLAR",
                table: "HOURLY_SHARES",
                columns: new[] { "SettlementRunId", "Hour" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MEMBER_SETTLEMENTS_SettlementRunId_MemberId",
                schema: "SOLAR",
                table: "MEMBER_SETTLEMENTS",
                columns: new[] { "SettlementRunId", "MemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SETTLEMENT_RUNS_COMMUNITY_ID_PERIOD_START_UTC_PERIOD_END_UTC",
                schema: "SOLAR",
                table: "SETTLEMENT_RUNS",
                columns: new[] { "COMMUNITY_ID", "PERIOD_START_UTC", "PERIOD_END_UTC" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SETTLEMENT_RUNS_STATUS",
                schema: "SOLAR",
                table: "SETTLEMENT_RUNS",
                column: "STATUS");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HOURLY_SHARES",
                schema: "SOLAR");

            migrationBuilder.DropTable(
                name: "MEMBER_SETTLEMENTS",
                schema: "SOLAR");

            migrationBuilder.DropTable(
                name: "SETTLEMENT_RUNS",
                schema: "SOLAR");
        }
    }
}
