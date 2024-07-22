using Microsoft.EntityFrameworkCore;


namespace APILPNPicking.data
{
    public class LPNPickingContext : DbContext
    { 
        public LPNPickingContext(DbContextOptions<LPNPickingContext> options)
            : base(options)
        {
        }
        
        public DbSet<LPNSorting> LPNSorting { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<LPNSorting>(entity =>
            {
                entity.ToTable("LPNSorting");

                entity.HasKey(e => e.idLPNSorting);

                entity.Property(e => e.idLPNSorting)
                    .HasColumnName("idLPNSorting")
                    .ValueGeneratedOnAdd(); 

                entity.Property(e => e.Wave)
                    .HasMaxLength(50)  
                    .HasColumnName("Wave");

                entity.Property(e => e.IdOrdenTrabajo)
                    .HasMaxLength(50) 
                    .HasColumnName("IdOrdenTrabajo");

                entity.Property(e => e.CodProducto)
                    .HasMaxLength(50)  
                    .HasColumnName("CodProducto");

                entity.Property(e => e.CantidadUnidades)
                    .HasColumnName("CantidadUnidades");

                entity.Property(e => e.DtlNumber)
                    .HasMaxLength(50) 
                    .HasColumnName("DtlNumber");

            });
        }
    }
}
