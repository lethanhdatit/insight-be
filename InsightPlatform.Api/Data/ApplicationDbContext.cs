using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Pain> Pains { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<LuckyNumberProvider> LuckyNumberProviders { get; set; }
    public DbSet<LuckyNumberRecord> LuckyNumberRecords { get; set; }
    public DbSet<LuckyNumberRecordByKind> LuckyNumberRecordByKinds { get; set; }
    public DbSet<TheologyRecord> TheologyRecords { get; set; }
    public DbSet<FatePointTransaction> FatePointTransactions { get; set; }
    public DbSet<ServicePrice> ServicePrices { get; set; }
    public DbSet<TopUpPackage> TopUpPackages { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<EntityOTP> EntityOTPs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Pain>()
                    .HasOne(p => p.User)
                    .WithMany(u => u.Pains)
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<LuckyNumberRecord>(entity =>
        {
            entity.Property(e => e.Detail).HasColumnType("jsonb");

            entity.HasOne(p => p.Provider)
                    .WithMany(u => u.Records)
                    .HasForeignKey(p => p.ProviderId)
                    .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TheologyRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AutoId).UseIdentityColumn();
            entity.Property(e => e.Input).HasColumnType("jsonb");
            entity.Property(e => e.PreData).HasColumnType("jsonb");
            entity.Property(e => e.Result).HasColumnType("jsonb");
            entity.Property(e => e.ServicePriceSnap).HasColumnType("jsonb");

            entity.HasOne(p => p.User)
                    .WithMany(u => u.TheologyRecords)
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.Property(e => e.ProviderTransaction).HasColumnType("jsonb");
            entity.Property(e => e.MetaData).HasColumnType("jsonb");

            entity.HasOne(t => t.User)
                  .WithMany(u => u.Transactions)
                  .HasForeignKey(t => t.UserId);

            entity.HasOne(t => t.TopUpPackage)
                 .WithMany(u => u.Transactions)
                 .HasForeignKey(t => t.TopUpPackageId);
        });

        modelBuilder.Entity<FatePointTransaction>()
            .HasOne(f => f.User)
            .WithMany(u => u.FatePointTransactions)
            .HasForeignKey(f => f.UserId);

        modelBuilder.Entity<FatePointTransaction>()
            .HasOne(f => f.Transaction)
            .WithMany(u => u.FatePointTransactions)
            .HasForeignKey(f => f.TransactionId);

        modelBuilder.Entity<FatePointTransaction>()
            .HasOne(f => f.TheologyRecord)
            .WithMany(u => u.FatePointTransactions)
            .HasForeignKey(f => f.TheologyRecordId);

        modelBuilder.Entity<ServicePrice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AutoId).UseIdentityColumn();
        });

        modelBuilder.Entity<TheologyRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AutoId).UseIdentityColumn();
        });

        modelBuilder.Entity<TopUpPackage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AutoId).UseIdentityColumn();
        });
        
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AutoId).UseIdentityColumn();
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is Trackable && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var trackable = (Trackable)entry.Entity;
            var utcNow = DateTime.UtcNow;

            if (entry.State == EntityState.Added)
            {
                trackable.CreatedTs = utcNow;
                trackable.LastUpdatedTs = null;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(Trackable.CreatedTs)).IsModified = false;
                trackable.LastUpdatedTs = utcNow;
            }
        }
    }
}