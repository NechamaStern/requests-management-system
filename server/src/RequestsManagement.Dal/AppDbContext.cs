using Microsoft.EntityFrameworkCore;
using RequestsManagement.Dal.Entities;

namespace RequestsManagement.Dal;

// The schema is created by the scripts in /database; EF Core only maps to it (no migrations).
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Request> Requests => Set<Request>();
    public DbSet<RequestStatusHistory> RequestStatusHistory => Set<RequestStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.Property(x => x.UserName).HasMaxLength(100);
            e.Property(x => x.DisplayName).HasMaxLength(200);
            e.Property(x => x.Email).HasMaxLength(256);
            e.HasIndex(x => x.UserName).IsUnique();
        });

        modelBuilder.Entity<Request>(e =>
        {
            e.ToTable("Requests");
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.OrganizationName).HasMaxLength(200);
            e.Property(x => x.Status).HasConversion<byte>();
            e.Property(x => x.Priority).HasConversion<byte>();
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne(x => x.AssignedToUser).WithMany()
                .HasForeignKey(x => x.AssignedToUserId);
        });

        modelBuilder.Entity<RequestStatusHistory>(e =>
        {
            e.ToTable("RequestStatusHistory");
            e.Property(x => x.PreviousStatus).HasConversion<byte>();
            e.Property(x => x.NewStatus).HasConversion<byte>();
            e.HasOne<Request>().WithMany().HasForeignKey(x => x.RequestId);
            e.HasOne(x => x.ChangedByUser).WithMany()
                .HasForeignKey(x => x.ChangedByUserId);
        });
    }
}

