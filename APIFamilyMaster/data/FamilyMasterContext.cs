using Microsoft.EntityFrameworkCore;

namespace APIFamilyMaster.data
{
    public class FamilyMasterContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var connectionString = Environment.GetEnvironmentVariable("DefaultConnection");
                optionsBuilder.UseSqlServer(connectionString);
            }
        }
        public FamilyMasterContext(DbContextOptions<FamilyMasterContext> options) : base(options) 
        { 
        }

        public DbSet<FamilyMaster> Familias { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FamilyMaster>()
                        .ToTable("FamilyMaster"); 

            modelBuilder.Entity<FamilyMaster>(entity =>
            {
                entity.HasKey(e => e.IdFamilyMaster);

                entity.Property(e => e.Familia)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.NumSalida)
                    .IsRequired();

                entity.Property(e => e.NumTanda)
                    .IsRequired();
                entity.Property(e => e.Tienda1)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Tienda2)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Tienda3)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Tienda4)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Tienda5)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Tienda6)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Tienda7)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Tienda8)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Tienda9)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Tienda10)
                    .IsRequired()
                    .HasMaxLength(255);

                //entity.Property(e => e.Tienda11)
                 //   .HasMaxLength(255);

                //entity.Property(e => e.Tienda12)
                  //  .HasMaxLength(255);

                //entity.Property(e => e.Tienda13)
                  //  .HasMaxLength(255);

              //  entity.Property(e => e.Tienda14)
                //    .HasMaxLength(255);

                entity.Property(e => e.IdFamilyMaster)
                    .ValueGeneratedOnAdd();
            });

            base.OnModelCreating(modelBuilder);
        }

    }
}
