using Microsoft.EntityFrameworkCore;
using NasIndexer.Model;

namespace NasIndexer.Data
{
    public class NasIndexerDbContext : DbContext
    {
        public NasIndexerDbContext(DbContextOptions<NasIndexerDbContext> options) : base(options)
        {
        }

        public DbSet<NasServer> NasServers { get; set; } = null!;
        public DbSet<ScanJob> ScanJobs { get; set; } = null!;
        public DbSet<DirectoryItem> DirectoryItems { get; set; } = null!;
        public DbSet<FileItem> FileItems { get; set; } = null!;
        public DbSet<FileTag> FileTags { get; set; } = null!;
        public DbSet<FileChangeLog> FileChangeLogs { get; set; } = null!;
        public DbSet<SystemAdmin> SystemAdmins { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<NasServer>().ToTable("NasServers");
            modelBuilder.Entity<ScanJob>().ToTable("ScanJobs");
            modelBuilder.Entity<DirectoryItem>().ToTable("DirectoryItems");
            modelBuilder.Entity<FileItem>().ToTable("FileItems");
            modelBuilder.Entity<FileTag>().ToTable("FileTags");
            modelBuilder.Entity<FileChangeLog>().ToTable("FileChangeLogs");
            modelBuilder.Entity<SystemAdmin>().ToTable("SystemAdmins");

            modelBuilder.Entity<NasServer>()
                .HasMany(server => server.ScanJobs)
                .WithOne(scanJob => scanJob.NasServer)
                .HasForeignKey(scanJob => scanJob.NasServerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ScanJob>()
                .HasMany(scanJob => scanJob.ScannedDirectories)
                .WithOne(directory => directory.ScanJob)
                .HasForeignKey(directory => directory.ScanJobId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DirectoryItem>()
                .HasOne(directory => directory.Parent)
                .WithMany(directory => directory.SubDirectories)
                .HasForeignKey(directory => directory.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DirectoryItem>()
                .HasMany(directory => directory.Files)
                .WithOne(file => file.Directory)
                .HasForeignKey(file => file.DirectoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FileItem>()
                .HasMany(file => file.ChangeLogs)
                .WithOne(changeLog => changeLog.File)
                .HasForeignKey(changeLog => changeLog.FileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FileItem>()
                .HasMany(file => file.Tags)
                .WithMany(tag => tag.Files)
                .UsingEntity(join => join.ToTable("FileItemTags"));

            modelBuilder.Entity<SystemAdmin>()
                .HasMany(admin => admin.ManagedServers)
                .WithMany(server => server.ManagedAdmins)
                .UsingEntity(join => join.ToTable("SystemAdminNasServers"));
        }
    }
}
