using Microsoft.EntityFrameworkCore;

namespace APIFamilyMaster.data
{
    public class FamilyMasterContext : DbContext
    {

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

                entity.Property(e => e.IdFamilyMaster)
                    .ValueGeneratedOnAdd();
            });

            base.OnModelCreating(modelBuilder);
        }

    }
}
