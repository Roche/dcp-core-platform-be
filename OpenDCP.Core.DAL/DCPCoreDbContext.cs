using OpenDCP.Core.DAL.Entities;
using OpenDCP.Framework.AuditTrail.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenDCP.Core.DAL
{
    public class DCPCoreDbContext : DbContext
    {

        private static int _instanceCounter = 0;
        private readonly int _instanceId;

        public DCPCoreDbContext(DbContextOptions<DCPCoreDbContext> options)
          : base(options)
        {
            // Ensure Invariant Culture is used
            _instanceId = ++_instanceCounter;
            Console.WriteLine($"DbContext Instance {_instanceId} Created");
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        }

        public virtual DbSet<Site> Site { get; set; }
        public virtual DbSet<Module> Module { get; set; }
        public virtual DbSet<DataSource> DataSource { get; set; }
        public virtual DbSet<DataType> DataType { get; set; }
        public virtual DbSet<ConnectionBridge> ConnectionBridge { get; set; }
        //Auth
        public virtual DbSet<Client> Client { get; set; }
        public virtual DbSet<UserClient> UserClient { get; set; }
        public virtual DbSet<User> User { get; set; }
        public virtual DbSet<PersistedGrant> PersitedGrant { get; set; }
        public virtual DbSet<TrustedCertificate> TrustedCertificate { get; set; }
        //Banners
        public virtual DbSet<UserInfoBanner> UserInfoBanner { get; set; }
        //Sharing
        public virtual DbSet<ShareLink> ShareLink { get; set; }
        public virtual DbSet<ShareLinkSystemGenerated> ShareLinkSystemGenerated { get; set; }
        public virtual DbSet<ShareLinkAlias> ShareLinkAlias { get; set; }
        //AuditTrail
        public virtual DbSet<AuditTrailAdminAction> AuditTrailAdminAction { get; set; }
        public virtual DbSet<SecurityUserLog> SecurityUserLog { get; set; }
        public virtual DbSet<SecuritySystemLog> SecuritySystemLog { get; set; }
        //Batch/Eventframe subscribtion
        public virtual DbSet<InProgressEvent> InProgressEvent { get; set; }
        public virtual DbSet<RefreshToken> RefreshToken { get; set; }
        //Dashboard
        public virtual DbSet<DashboardTab> DashboardTab { get; set; }
        public virtual DbSet<DashboardExport> DashboardExport { get; set; }
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<string>().UseCollation("case_insensitive");

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserClient>(entity =>
            {
                entity.Property(e => e.SysStartTime)
                     .ValueGeneratedOnAdd()
                     .HasDefaultValueSql("now()")
                     .HasColumnType("timestamptz");

                entity.Property(e => e.SysEndTime)
                      .ValueGeneratedOnAddOrUpdate()
                      .HasDefaultValueSql("'infinity'")
                      .HasColumnType("timestamptz");
            });

            modelBuilder.HasCollation(
                "case_insensitive",
                locale: "en-u-ks-primary",
                provider: "icu",
                deterministic: false
            );

            modelBuilder.Entity<Site>(entity =>
            {
                entity.Property(e => e.SysStartTime)
                     .ValueGeneratedOnAdd()
                     .HasColumnType("timestamptz");

                entity.Property(e => e.SysEndTime)
                      .ValueGeneratedOnAddOrUpdate()
                      .HasColumnType("timestamptz");
            });

            modelBuilder.Entity<DataType>(entity =>
            {
                entity.Property(e => e.SysStartTime)
                     .ValueGeneratedOnAdd()
                     .HasColumnType("timestamptz");

                entity.Property(e => e.SysEndTime)
                      .ValueGeneratedOnAddOrUpdate()
                      .HasColumnType("timestamptz");
            });

            modelBuilder.Entity<DataSource>(entity =>
            {
                entity.Property(e => e.SysStartTime)
                     .ValueGeneratedOnAdd()
                     .HasColumnType("timestamptz");

                entity.Property(e => e.SysEndTime)
                      .ValueGeneratedOnAddOrUpdate()
                      .HasColumnType("timestamptz");
            });

            modelBuilder.Entity<Module>(entity =>
            {
                entity.Property(e => e.SysStartTime)
                     .ValueGeneratedOnAdd()
                     .HasColumnType("timestamptz");

                entity.Property(e => e.SysEndTime)
                      .ValueGeneratedOnAddOrUpdate()
                      .HasColumnType("timestamptz");
            });

            modelBuilder.Entity<ConnectionBridge>(entity =>
            {
                entity.Property(e => e.SysStartTime)
                     .ValueGeneratedOnAdd()
                     .HasColumnType("timestamptz");

                entity.Property(e => e.SysEndTime)
                      .ValueGeneratedOnAddOrUpdate()
                      .HasColumnType("timestamptz");
            });

            modelBuilder.Entity<UserInfoBanner>(entity =>
            {
                entity.Property(e => e.SysStartTime)
                     .ValueGeneratedOnAdd()
                     .HasColumnType("timestamptz");

                entity.Property(e => e.SysEndTime)
                      .ValueGeneratedOnAddOrUpdate()
                      .HasColumnType("timestamptz");
            });

            modelBuilder.Entity<ShareLinkAlias>(entity =>
            {
                entity.Property(e => e.SysStartTime)
                     .ValueGeneratedOnAdd()
                     .HasColumnType("timestamptz");

                entity.Property(e => e.SysEndTime)
                      .ValueGeneratedOnAddOrUpdate()
                      .HasColumnType("timestamptz");
            });

            modelBuilder.Entity<ShareLink>(entity =>
            {
                entity.Property(e => e.SysStartTime)
                     .ValueGeneratedOnAdd()
                     .HasColumnType("timestamptz");

                entity.Property(e => e.SysEndTime)
                      .ValueGeneratedOnAddOrUpdate()
                      .HasColumnType("timestamptz");
            });

            modelBuilder.Entity<DashboardTab>(entity =>
            {
                entity.Property(e => e.SysStartTime)
                     .ValueGeneratedOnAdd()
                     .HasColumnType("timestamptz");

                entity.Property(e => e.SysEndTime)
                      .ValueGeneratedOnAddOrUpdate()
                      .HasColumnType("timestamptz");
            });

            modelBuilder.Entity<DashboardExport>(entity =>
            {
                entity.Property(e => e.SysStartTime)
                     .ValueGeneratedOnAdd()
                     .HasColumnType("timestamptz");

                entity.Property(e => e.SysEndTime)
                      .ValueGeneratedOnAddOrUpdate()
                      .HasColumnType("timestamptz");
            });

            modelBuilder.Entity<InProgressEvent>().OwnsMany(model => model.JEventValues,
                ownedNavigationBuilder =>
            {
                ownedNavigationBuilder.ToJson();
            });

            modelBuilder.Entity<User>().OwnsMany(model => model.Permissions,
                ownedNavigationBuilder =>
            {
                ownedNavigationBuilder.ToJson();
            });

            modelBuilder.Entity<UserClient>().OwnsMany(model => model.Permissions,
                ownedNavigationBuilder =>
            {
                ownedNavigationBuilder.ToJson();
            });

            modelBuilder.Entity<TrustedCertificate>().OwnsMany(model => model.Permissions,
                ownedNavigationBuilder =>
                {
                    ownedNavigationBuilder.ToJson();
                });

            modelBuilder.Entity<ConnectionBridge>()
             .HasOne(cb => cb.SCon)
             .WithMany()
             .HasForeignKey(cb => cb.SConId)
             .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ConnectionBridge>()
                .Property<string>("_definitionJSON")
                .HasColumnName("DefinitionJSON")
                .IsRequired();


            modelBuilder.Entity<ShareLink>().Property(x => x.JLink).HasConversion
             (
                  x => x.ToJsonString(null),
                  x => string.IsNullOrWhiteSpace(x)
                         ? new JsonObject(null)
                         : JsonSerializer.Deserialize<JsonObject>(x, new JsonSerializerOptions()) ?? new JsonObject(null),
                   new ValueComparer<JsonObject>(
                        (c1, c2) => ReferenceEquals(c1, c2) || (c1 != null && c2 != null && c1.Equals(c2)),
                        c => c != null ? c.GetHashCode() : 0,
                        c => c != null ? c : new JsonObject(null)
                   )
             );

            modelBuilder.Entity<ShareLinkSystemGenerated>().Property(x => x.JLink).HasConversion
             (
                  x => x.ToJsonString(null),
                  x => string.IsNullOrWhiteSpace(x)
                         ? new JsonObject(null)
                         : JsonSerializer.Deserialize<JsonObject>(x, new JsonSerializerOptions()) ?? new JsonObject(null),
                   new ValueComparer<JsonObject>(
                        (c1, c2) => ReferenceEquals(c1, c2) || (c1 != null && c2 != null && c1.Equals(c2)),
                        c => c != null ? c.GetHashCode() : 0,
                        c => c != null ? c : new JsonObject(null)
                   )
             );

            //Views
            modelBuilder.Entity<AuditEntryDTO>(x =>
            {
                x.HasNoKey();
                x.ToView(null);
            });

        }
    }
}
