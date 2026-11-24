using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenDCP.Core.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardTabAndDashboardExportTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DashboardTab",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false, collation: "case_insensitive"),
                    SiteId = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    LastModifiedBy = table.Column<int>(type: "integer", nullable: true),
                    OwnerUserId = table.Column<int>(type: "integer", nullable: true),
                    SysStartTime = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    SysEndTime = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardTab", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DashboardExport",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DashboardExportGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    SiteId = table.Column<int>(type: "integer", nullable: false),
                    DashboardTabId = table.Column<Guid>(type: "uuid", nullable: false),
                    DashboardTabVersion = table.Column<int>(type: "integer", nullable: false),
                    ExportDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<int>(type: "integer", nullable: true),
                    OwnerUserId = table.Column<int>(type: "integer", nullable: true),
                    SysStartTime = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    SysEndTime = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardExport", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DashboardExport_DashboardTab_DashboardTabId",
                        column: x => x.DashboardTabId,
                        principalTable: "DashboardTab",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardExport_DashboardTabId",
                table: "DashboardExport",
                column: "DashboardTabId");

            migrationBuilder.Sql(
                "SELECT periods.add_system_time_period('public.\"DashboardTab\"', 'SysStartTime', 'SysEndTime');");

            migrationBuilder.Sql(
                "SELECT periods.add_system_versioning('public.\"DashboardTab\"');");

            migrationBuilder.Sql(
                "SELECT periods.add_system_time_period('public.\"DashboardExport\"', 'SysStartTime', 'SysEndTime');");

            migrationBuilder.Sql(
                "SELECT periods.add_system_versioning('public.\"DashboardExport\"');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
            "SELECT periods.drop_system_versioning('\"DashboardTab\"');");

            migrationBuilder.Sql(
            "SELECT periods.drop_system_versioning('\"DashboardExport\"');");

            migrationBuilder.DropTable(
                name: "DashboardExport");

            migrationBuilder.DropTable(
                name: "DashboardTab");
        }
    }
}
