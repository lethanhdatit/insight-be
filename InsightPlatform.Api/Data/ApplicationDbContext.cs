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
    }
}