using EmailClassifier.Models;
using Microsoft.EntityFrameworkCore;

namespace EmailClassifier.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Company> Companies { get; set; }
    public DbSet<Contact> Contacts { get; set; }
    public DbSet<EmailInbox> EmailInboxes { get; set; }
    public DbSet<ProcessingLog> ProcessingLogs { get; set; }
    public DbSet<WorkerState> WorkerStates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmailInbox>().Property(e => e.Status).HasConversion<string>();

        modelBuilder.Entity<WorkerState>().HasIndex(w => w.Key).IsUnique();

        modelBuilder
            .Entity<Contact>()
            .HasOne(c => c.Company)
            .WithMany(co => co.Contacts)
            .HasForeignKey(c => c.CompanyId);

        modelBuilder
            .Entity<EmailInbox>()
            .HasOne(e => e.Contact)
            .WithMany(c => c.Emails)
            .HasForeignKey(e => e.ContactId)
            .IsRequired(false);
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Properties.Any(p => p.Metadata.Name == "UpdatedAt"))
                entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;

            if (
                entry.State == EntityState.Added
                && entry.Properties.Any(p => p.Metadata.Name == "CreatedAt")
            )
                entry.Property("CreatedAt").CurrentValue = DateTime.UtcNow;
        }
    }
}
