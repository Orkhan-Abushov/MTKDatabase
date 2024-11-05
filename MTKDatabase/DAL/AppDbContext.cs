using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MTKDatabase.Models;

namespace MTKDatabase.DAL
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }
        public DbSet<Complex> Complexes { get; set; }
        public DbSet<ManagementBoard> ManagementBoards { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Comment>(entity =>
            {
                entity.Property(e => e.Name).HasColumnType("varchar(100)").IsRequired(false);
                entity.Property(e => e.PhoneNumber).HasColumnType("varchar(255)").IsRequired();
                entity.Property(e => e.Email).HasColumnType("varchar(255)").IsRequired(false);
                entity.Property(e => e.Description).HasColumnType("varchar(2500)").IsRequired(false);
                entity.Property(e => e.CreatedDate).HasColumnType("datetime").IsRequired();
                entity.Property(e => e.IsActive).HasColumnType("bit").IsRequired();
            });

            // Define unique index on the Username property with case sensitivity
            modelBuilder.Entity<ManagementBoard>()
                .HasIndex(m => m.Username)
                .IsUnique();

            // Ensure the Username column is case-sensitive (using SQL Server collation)
            modelBuilder.Entity<ManagementBoard>()
                .Property(m => m.Username)
                .HasColumnType("VARCHAR(100)")  // Use VARCHAR instead of NVARCHAR for case sensitivity
                .UseCollation("Latin1_General_BIN");  // Binary collation enforces case sensitivity

            base.OnModelCreating(modelBuilder);
        }
        public DbSet<Merchant> Merchants { get; set; }
        public DbSet<LatestNews> LatestNews { get; set; }
        public DbSet<Comment> Comments { get; set; }
    }
}
