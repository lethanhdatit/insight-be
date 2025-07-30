using Microsoft.EntityFrameworkCore;

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
    }
}