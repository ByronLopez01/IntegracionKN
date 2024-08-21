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
        public DbSet<OrdenEnProceso> ordenesEnProceso { get; set; }

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

            // Configuración para OrdenEnProceso
            modelBuilder.Entity<OrdenEnProceso>(entity =>
            {
                entity.ToTable("OrdenEnProceso");

                entity.HasKey(e => e.id);

                entity.Property(e => e.id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.codMastr)
                    .HasMaxLength(50)
                    .HasColumnName("codMastr");

                entity.Property(e => e.codInr)
                    .HasMaxLength(50)
                    .HasColumnName("codInr");

                entity.Property(e => e.cantMastr)
                    .HasColumnName("cantMastr");

                entity.Property(e => e.cantInr)
                    .HasColumnName("cantInr");

                entity.Property(e => e.cantidad)
                    .HasColumnName("cantidad");

                entity.Property(e => e.familia)
                    .HasMaxLength(50)
                    .HasColumnName("familia");

                entity.Property(e => e.numOrden)
                    .HasMaxLength(50)
                    .HasColumnName("numOrden");

                entity.Property(e => e.codProducto)
                    .HasMaxLength(50)
                    .HasColumnName("codProducto");

                entity.Property(e => e.wave)
                    .HasMaxLength(50)
                    .HasColumnName("wave");

                entity.Property(e => e.cantidadProcesada)
                    .HasColumnName("cantidadProcesada");

                entity.Property(e => e.cantidadLPN)
                    .HasColumnName("cantidadLPN");

                entity.Property(e => e.numSalida)
                    .HasColumnName("numSalida");

                entity.Property(e => e.numTanda)
                    .HasColumnName("numTanda");

                entity.Property(e => e.dtlNumber)
                   .HasColumnName("dtlNumber");

                entity.Property(e => e.estado)
                   .HasMaxLength(50)
                   .HasColumnName("estado");

                entity.Property(e => e.tienda)
                    .HasMaxLength(50)
                    .HasColumnName("tienda");

                
            });
        }
    }
}
