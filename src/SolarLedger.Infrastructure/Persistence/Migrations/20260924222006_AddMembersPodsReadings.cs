using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SolarLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMembersPodsReadings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MEMBERS",
                schema: "SOLAR",
                columns: table => new
                {
                    Id = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    CommunityId = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    PaymentReference = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MEMBERS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MEMBERS_COMMUNITIES_CommunityId",
                        column: x => x.CommunityId,
                        principalSchema: "SOLAR",
                        principalTable: "COMMUNITIES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PODS",
                schema: "SOLAR",
                columns: table => new
                {
                    Id = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    MemberId = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    PodCode = table.Column<string>(type: "NVARCHAR2(32)", maxLength: 32, nullable: false),
                    Type = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PODS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PODS_MEMBERS_MemberId",
                        column: x => x.MemberId,
                        principalSchema: "SOLAR",
                        principalTable: "MEMBERS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ENERGY_READINGS",
                schema: "SOLAR",
                columns: table => new
                {
                    Id = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    PodId = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    Hour = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    InjectedKwh = table.Column<decimal>(type: "DECIMAL(18,3)", precision: 18, scale: 3, nullable: false),
                    WithdrawnKwh = table.Column<decimal>(type: "DECIMAL(18,3)", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ENERGY_READINGS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ENERGY_READINGS_PODS_PodId",
                        column: x => x.PodId,
                        principalSchema: "SOLAR",
                        principalTable: "PODS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ENERGY_READINGS_PodId_Hour",
                schema: "SOLAR",
                table: "ENERGY_READINGS",
                columns: new[] { "PodId", "Hour" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MEMBERS_CommunityId",
                schema: "SOLAR",
                table: "MEMBERS",
                column: "CommunityId");

            migrationBuilder.CreateIndex(
                name: "IX_PODS_MemberId",
                schema: "SOLAR",
                table: "PODS",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_PODS_PodCode",
                schema: "SOLAR",
                table: "PODS",
                column: "PodCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ENERGY_READINGS",
                schema: "SOLAR");

            migrationBuilder.DropTable(
                name: "PODS",
                schema: "SOLAR");

            migrationBuilder.DropTable(
                name: "MEMBERS",
                schema: "SOLAR");
        }
    }
}
