using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SolarLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "SOLAR");

            migrationBuilder.CreateTable(
                name: "COMMUNITIES",
                schema: "SOLAR",
                columns: table => new
                {
                    Id = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    PrimarySubstationCode = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    IncentiveTariffEurPerMwh = table.Column<decimal>(type: "NUMBER(18,3)", nullable: false),
                    DistributionPolicy = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_COMMUNITIES", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_COMMUNITIES_PrimarySubstationCode",
                schema: "SOLAR",
                table: "COMMUNITIES",
                column: "PrimarySubstationCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "COMMUNITIES",
                schema: "SOLAR");
        }
    }
}
