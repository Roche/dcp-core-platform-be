using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenDCP.Core.DAL.Migrations
{
    /// <inheritdoc />
    public partial class Move2PostgreSQL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:case_insensitive", "en-u-ks-primary,en-u-ks-primary,icu,False");

            migrationBuilder.CreateTable(
                name: "AuditTrailAdminAction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActionConfiguration = table.Column<string>(type: "text", nullable: true, collation: "case_insensitive"),
                    Reason = table.Column<string>(type: "text", nullable: false, collation: "case_insensitive"),
                    Module = table.Column<string>(type: "text", nullable: false, collation: "case_insensitive"),
                    Exception = table.Column<string>(type: "text", nullable: true, collation: "case_insensitive")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditTrailAdminAction", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Client",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<string>(type: "text", nullable: false, collation: "case_insensitive"),
                    ClientSecret = table.Column<string>(type: "text", nullable: false, collation: "case_insensitive"),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, collation: "case_insensitive"),
                    Description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false, collation: "case_insensitive"),
                    ApplicationType = table.Column<byte>(type: "smallint", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    RefreshTokenLifeTime = table.Column<int>(type: "integer", nullable: false),
                    AllowedOrigin = table.Column<string>(type: "text", nullable: false, collation: "case_insensitive"),
                    Permissions = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Client", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersitedGrant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UnixId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, collation: "case_insensitive"),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Expiration = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<byte>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersitedGrant", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefreshToken",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserIdentifier = table.Column<string>(type: "text", nullable: true, collation: "case_insensitive"),
                    Token = table.Column<string>(type: "text", nullable: true, collation: "case_insensitive"),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshToken", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecuritySystemLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, collation: "case_insensitive"),
                    Action = table.Column<byte>(type: "smallint", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecuritySystemLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecurityUserLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UnixId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, collation: "case_insensitive"),
                    Action = table.Column<byte>(type: "smallint", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityUserLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Site",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false, collation: "case_insensitive"),
                    ShortName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false, collation: "case_insensitive"),
                    TimeZone = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false, collation: "case_insensitive"),
                    IsEnable = table.Column<bool>(type: "boolean", nullable: false),
                    SiteType = table.Column<byte>(type: "smallint", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false, collation: "case_insensitive"),
                    LastModifiedBy = table.Column<int>(type: "integer", nullable: true),
                    OwnerUserId = table.Column<int>(type: "integer", nullable: true),
                    SysStartTime = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    SysEndTime = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Site", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UnixId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, collation: "case_insensitive"),
                    DisplayName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false, collation: "case_insensitive"),
                    FirstName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false, collation: "case_insensitive"),
                    LastName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false, collation: "case_insensitive"),
                    Email = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false, collation: "case_insensitive"),
                    CurrentSiteId = table.Column<int>(type: "integer", nullable: true),
                    TimeFormat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, collation: "case_insensitive"),
                    DateFormat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, collation: "case_insensitive"),
                    LastLogin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Permissions = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserInfoBanner",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false, collation: "case_insensitive"),
                    Category = table.Column<byte>(type: "smallint", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false, collation: "case_insensitive"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SiteId = table.Column<int>(type: "integer", nullable: true),
                    ModuleName = table.Column<string>(type: "text", nullable: true, collation: "case_insensitive"),
                    LastModifiedBy = table.Column<int>(type: "integer", nullable: true),
                    OwnerUserId = table.Column<int>(type: "integer", nullable: true),
                    SysStartTime = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    SysEndTime = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInfoBanner", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataSource",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false, collation: "case_insensitive"),
                    Type = table.Column<string>(type: "text", nullable: false, collation: "case_insensitive"),
                    ConnectionString = table.Column<string>(type: "text", nullable: false, collation: "case_insensitive"),
                    SiteId = table.Column<int>(type: "integer", nullable: false),
                    IsValidated = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false, collation: "case_insensitive"),
                    LastModifiedBy = table.Column<int>(type: "integer", nullable: true),
                    OwnerUserId = table.Column<int>(type: "integer", nullable: true),
                    SysStartTime = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    SysEndTime = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSource", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataSource_Site_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Site",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Module",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false, collation: "case_insensitive"),
                    Type = table.Column<byte>(type: "smallint", nullable: false),
                    ConnectionString = table.Column<string>(type: "text", nullable: false, collation: "case_insensitive"),
                    SiteId = table.Column<int>(type: "integer", nullable: false),
                    IsEnable = table.Column<bool>(type: "boolean", nullable: false),
                    IsValidated = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false, collation: "case_insensitive"),
                    LastModifiedBy = table.Column<int>(type: "integer", nullable: true),
                    OwnerUserId = table.Column<int>(type: "integer", nullable: true),
                    SysStartTime = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    SysEndTime = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Module", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Module_Site_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Site",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShareLink",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JLink = table.Column<string>(type: "text", nullable: false),
                    LinkGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false, collation: "case_insensitive"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    LastModifiedBy = table.Column<int>(type: "integer", nullable: true),
                    OwnerUserId = table.Column<int>(type: "integer", nullable: true),
                    SiteId = table.Column<int>(type: "integer", nullable: true),
                    SysStartTime = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    SysEndTime = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShareLink", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShareLink_Site_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Site",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ShareLinkSystemGenerated",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JLink = table.Column<string>(type: "text", nullable: false),
                    LinkGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SiteId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShareLinkSystemGenerated", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShareLinkSystemGenerated_Site_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Site",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserClient",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<string>(type: "text", nullable: false, collation: "case_insensitive"),
                    ClientSecret = table.Column<string>(type: "text", nullable: false, collation: "case_insensitive"),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, collation: "case_insensitive"),
                    Description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false, collation: "case_insensitive"),
                    Status = table.Column<byte>(type: "smallint", nullable: false),
                    OwnerUserId = table.Column<int>(type: "integer", nullable: false),
                    LastModifiedBy = table.Column<int>(type: "integer", nullable: true),
                    SysStartTime = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    SysEndTime = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "'infinity'"),
                    Permissions = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClient", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClient_User_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConnectionBridge",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false, collation: "case_insensitive"),
                    Strategy = table.Column<byte>(type: "smallint", nullable: false),
                    SConDataType = table.Column<byte>(type: "smallint", nullable: false),
                    TConDataType = table.Column<byte>(type: "smallint", nullable: false),
                    TConId = table.Column<int>(type: "integer", nullable: false),
                    SConId = table.Column<int>(type: "integer", nullable: false),
                    DefinitionJSON = table.Column<string>(type: "text", nullable: false, collation: "case_insensitive"),
                    Reason = table.Column<string>(type: "text", nullable: false, collation: "case_insensitive"),
                    LastModifiedBy = table.Column<int>(type: "integer", nullable: true),
                    OwnerUserId = table.Column<int>(type: "integer", nullable: true),
                    SysStartTime = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    SysEndTime = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionBridge", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConnectionBridge_DataSource_SConId",
                        column: x => x.SConId,
                        principalTable: "DataSource",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConnectionBridge_DataSource_TConId",
                        column: x => x.TConId,
                        principalTable: "DataSource",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DataType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<byte>(type: "smallint", nullable: false),
                    ModuleName = table.Column<string>(type: "text", nullable: false, collation: "case_insensitive"),
                    DataSourceId = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false, collation: "case_insensitive"),
                    LastModifiedBy = table.Column<int>(type: "integer", nullable: true),
                    OwnerUserId = table.Column<int>(type: "integer", nullable: true),
                    SysStartTime = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    SysEndTime = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataType", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataType_DataSource_DataSourceId",
                        column: x => x.DataSourceId,
                        principalTable: "DataSource",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InProgressEvent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<string>(type: "text", nullable: false, collation: "case_insensitive"),
                    EventStartTime = table.Column<DateTime>(type: "timestamp with time zone", maxLength: 50, nullable: false),
                    EventDisplayId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, collation: "case_insensitive"),
                    DeviceWebId = table.Column<string>(type: "text", nullable: false, collation: "case_insensitive"),
                    UpdateAttempts = table.Column<int>(type: "integer", nullable: false),
                    DatasourceId = table.Column<int>(type: "integer", nullable: false),
                    JEventValues = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InProgressEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InProgressEvent_DataSource_DatasourceId",
                        column: x => x.DatasourceId,
                        principalTable: "DataSource",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShareLinkAlias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false, collation: "case_insensitive"),
                    ShareLinkId = table.Column<int>(type: "integer", nullable: false),
                    LastModifiedBy = table.Column<int>(type: "integer", nullable: true),
                    OwnerUserId = table.Column<int>(type: "integer", nullable: true),
                    SysStartTime = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    SysEndTime = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShareLinkAlias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShareLinkAlias_ShareLink_ShareLinkId",
                        column: x => x.ShareLinkId,
                        principalTable: "ShareLink",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionBridge_SConId",
                table: "ConnectionBridge",
                column: "SConId");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionBridge_TConId",
                table: "ConnectionBridge",
                column: "TConId");

            migrationBuilder.CreateIndex(
                name: "IX_DataSource_SiteId",
                table: "DataSource",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_DataType_DataSourceId",
                table: "DataType",
                column: "DataSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_InProgressEvent_DatasourceId",
                table: "InProgressEvent",
                column: "DatasourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Module_SiteId",
                table: "Module",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareLink_SiteId",
                table: "ShareLink",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareLinkAlias_ShareLinkId",
                table: "ShareLinkAlias",
                column: "ShareLinkId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareLinkSystemGenerated_SiteId",
                table: "ShareLinkSystemGenerated",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_UserClient_OwnerUserId",
                table: "UserClient",
                column: "OwnerUserId");

            migrationBuilder.Sql(
                "SELECT periods.add_system_time_period('public.\"Site\"', 'SysStartTime', 'SysEndTime');");

            migrationBuilder.Sql(
                "SELECT periods.add_system_versioning('public.\"Site\"');");

            migrationBuilder.Sql(
                "SELECT periods.add_system_time_period('public.\"DataSource\"', 'SysStartTime', 'SysEndTime');");

            migrationBuilder.Sql(
                "SELECT periods.add_system_versioning('public.\"DataSource\"');");

            migrationBuilder.Sql(
                "SELECT periods.add_system_time_period('public.\"Module\"', 'SysStartTime', 'SysEndTime');");

            migrationBuilder.Sql(
                "SELECT periods.add_system_versioning('public.\"Module\"');");

            migrationBuilder.Sql(
                "SELECT periods.add_system_time_period('public.\"DataType\"', 'SysStartTime', 'SysEndTime');");

            migrationBuilder.Sql(
                "SELECT periods.add_system_versioning('public.\"DataType\"');");

            migrationBuilder.Sql(
                "SELECT periods.add_system_time_period('public.\"ConnectionBridge\"', 'SysStartTime', 'SysEndTime');");

            migrationBuilder.Sql(
                "SELECT periods.add_system_versioning('public.\"ConnectionBridge\"');");

            migrationBuilder.Sql(
                "SELECT periods.add_system_time_period('public.\"ShareLink\"', 'SysStartTime', 'SysEndTime');");

            migrationBuilder.Sql(
                "SELECT periods.add_system_versioning('public.\"ShareLink\"');");

            migrationBuilder.Sql(
                "SELECT periods.add_system_time_period('public.\"ShareLinkAlias\"', 'SysStartTime', 'SysEndTime');");

            migrationBuilder.Sql(
                "SELECT periods.add_system_versioning('public.\"ShareLinkAlias\"');");

            migrationBuilder.Sql(
                "SELECT periods.add_system_time_period('public.\"UserInfoBanner\"', 'SysStartTime', 'SysEndTime');");

            migrationBuilder.Sql(
                "SELECT periods.add_system_versioning('public.\"UserInfoBanner\"');");

            migrationBuilder.Sql(
                "SELECT periods.add_system_time_period('public.\"UserClient\"', 'SysStartTime', 'SysEndTime');");

            migrationBuilder.Sql(
                "SELECT periods.add_system_versioning('public.\"UserClient\"');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
            "SELECT periods.drop_system_versioning('\"Site\"');");

            migrationBuilder.Sql(
                 "SELECT periods.drop_system_versioning('\"DataSource\"');");

            migrationBuilder.Sql(
               "SELECT periods.drop_system_versioning('\"Module\"');");

            migrationBuilder.Sql(
               "SELECT periods.drop_system_versioning('\"DataType\"');");

            migrationBuilder.Sql(
               "SELECT periods.drop_system_versioning('\"ConnectionBridge\"');");

            migrationBuilder.Sql(
                "SELECT periods.drop_system_versioning('\"ShareLink\"');");

            migrationBuilder.Sql(
                "SELECT periods.drop_system_versioning('\"ShareLinkAlias\"');");

            migrationBuilder.Sql(
                "SELECT periods.drop_system_versioning('\"UserInfoBanner\"');");

            migrationBuilder.Sql(
                "SELECT periods.drop_system_versioning('\"UserClient\"');");

            migrationBuilder.DropTable(
                name: "AuditTrailAdminAction");

            migrationBuilder.DropTable(
                name: "Client");

            migrationBuilder.DropTable(
                name: "ConnectionBridge");

            migrationBuilder.DropTable(
                name: "DataType");

            migrationBuilder.DropTable(
                name: "InProgressEvent");

            migrationBuilder.DropTable(
                name: "Module");

            migrationBuilder.DropTable(
                name: "PersitedGrant");

            migrationBuilder.DropTable(
                name: "RefreshToken");

            migrationBuilder.DropTable(
                name: "SecuritySystemLog");

            migrationBuilder.DropTable(
                name: "SecurityUserLog");

            migrationBuilder.DropTable(
                name: "ShareLinkAlias");

            migrationBuilder.DropTable(
                name: "ShareLinkSystemGenerated");

            migrationBuilder.DropTable(
                name: "UserClient");

            migrationBuilder.DropTable(
                name: "UserInfoBanner");

            migrationBuilder.DropTable(
                name: "DataSource");

            migrationBuilder.DropTable(
                name: "ShareLink");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Site");

            migrationBuilder.DropTable(
                name: "Site_history");

            migrationBuilder.DropTable(
                name: "DataSource_history");

            migrationBuilder.DropTable(
                name: "DataType_history");

            migrationBuilder.DropTable(
                name: "Module_history");

            migrationBuilder.DropTable(
                name: "ShareLinkAlias_history");

            migrationBuilder.DropTable(
                name: "ShareLink_history");

            migrationBuilder.DropTable(
                name: "UserClient_history");

            migrationBuilder.DropTable(
                name: "UserInfoBanner_history");

            migrationBuilder.DropTable(
                name: "ConnectionBridge_history");
            
            migrationBuilder.Sql(
                "DROP COLLATION IF EXISTS case_insensitive;");
        }
    }
}
