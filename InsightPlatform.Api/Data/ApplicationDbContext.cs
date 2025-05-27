using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Pain> Pains { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
                
        modelBuilder.Entity<Pain>()
                    .HasOne(p => p.User)
                    .WithMany(u => u.Pains)
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
    }
}