namespace HierarchyComposer
{
    using Microsoft.EntityFrameworkCore;
    using Model;

    public class HierarchyContext : DbContext
    {
        public DbSet<Node> Nodes => Set<Node>();
        public DbSet<PDMSEntry> PdmsEntries => Set<PDMSEntry>();
        public DbSet<AABB> Aabbs => Set<AABB>();
        public DbSet<NodePDMSEntry> NodeToPDMSEntry => Set<NodePDMSEntry>();

        // This connection string is only used during manual migration from command line, use HierarchyContext(DbContextOptions)
        // constructor runtime.
        private const string DefaultMigrationConnectionString = @"Data Source = D:\tmp\Hierarchy.db;";

        public HierarchyContext()
        {
        }

        public HierarchyContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured)
                return;
            optionsBuilder
                .UseSqlite(DefaultMigrationConnectionString)
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<PDMSEntry>().ToTable("PDMSEntries");
            modelBuilder.Entity<AABB>().ToTable("AABBs");
            modelBuilder.Entity<NodePDMSEntry>().HasKey(e => new { e.NodeId, e.PDMSEntryId });
            modelBuilder.Entity<NodePDMSEntry>()
                .HasOne(e => e.Node)
                .WithMany(n => n.NodePDMSEntry)
                .HasForeignKey(e => e.NodeId);
            modelBuilder.Entity<NodePDMSEntry>()
                .HasOne(e => e.PDMSEntry)
                .WithMany(e => e.NodePDMSEntry)
                .HasForeignKey(e => e.PDMSEntryId);
            modelBuilder.Entity<Node>().ToTable("Nodes");

        }
    }
}