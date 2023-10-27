using Microsoft.EntityFrameworkCore;
using StorageProvidersSample.DataAccessLayer.Entities;

namespace StorageProvidersSample.DataAccessLayer;

public class DataContext : DbContext
{
    public DbSet<Photo> Photos { get; set; }

    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Photo>(builder =>
        {
            builder.ToTable("Photos");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).ValueGeneratedOnAdd().HasDefaultValueSql("newid()");

            builder.Property(p => p.FileName).HasMaxLength(256).IsRequired();
            builder.Property(p => p.Description).HasMaxLength(4000).IsRequired(false);
        });

        base.OnModelCreating(modelBuilder);
    }
}