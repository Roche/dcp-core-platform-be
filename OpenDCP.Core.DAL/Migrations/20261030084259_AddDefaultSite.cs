using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenDCP.Core.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultSite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Site",
                columns: new[]
                {
                    "Id",
                    "Name",
                    "ShortName",
                    "TimeZone",
                    "IsEnable",
                    "SiteType",
                    "Reason",
                    "LastModifiedBy",
                    "OwnerUserId",
                },
                values: new object[]
                {
                    0,
                    "Global",
                    "GLB",
                    "UTC",
                    true,
                    0,
                    string.Empty,
                    0,
                    0
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Site",
                keyColumn: "Id",
                keyValue: 0);
        }
    }
}
