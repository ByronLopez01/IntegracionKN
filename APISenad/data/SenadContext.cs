using Microsoft.EntityFrameworkCore;

namespace APISenad.data
{
    public class SenadContext : DbContext
    {
        public DbSet<OrdenEnProceso> ordenesEnProceso { get; set; }
        public DbSet<FamilyMaster> Familias { get; set; }
        public SenadContext(DbContextOptions<SenadContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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

                entity.Property(e => e.cantidad)
                    .HasMaxLength(50)
                    .HasColumnName("cantidad");


            });

            modelBuilder.Entity<FamilyMaster>(entity =>
            {
                entity.ToTable("FamilyMaster"); // Asegúrate de que este sea el nombre correcto de la tabla en tu base de datos

                entity.HasKey(e => e.IdFamilyMaster);

                entity.Property(e => e.IdFamilyMaster)
                    .HasColumnName("IdFamilyMaster")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.Familia)
                    .HasMaxLength(50)
                    .HasColumnName("Familia");

                entity.Property(e => e.NumSalida)
                    .HasColumnName("NumSalida");

                entity.Property(e => e.NumTanda)
                    .HasColumnName("NumTanda");

                entity.Property(e => e.Tienda1) // Agregar el resto de tiendas
                    .HasMaxLength(50)
                    .HasColumnName("Tienda1");

                entity.Property(e => e.Tienda2) // Agregar el resto de tiendas
                    .HasMaxLength(50)
                    .HasColumnName("Tienda2");

                entity.Property(e => e.Tienda3) // Agregar el resto de tiendas
                    .HasMaxLength(50)
                    .HasColumnName("Tienda3");

                entity.Property(e => e.Tienda4) // Agregar el resto de tiendas
                    .HasMaxLength(50)
                    .HasColumnName("Tienda4");

                entity.Property(e => e.Tienda5) // Agregar el resto de tiendas
                    .HasMaxLength(50)
                    .HasColumnName("Tienda5");

                entity.Property(e => e.Tienda6) // Agregar el resto de tiendas
                    .HasMaxLength(50)
                    .HasColumnName("Tienda6");

                entity.Property(e => e.Tienda7) // Agregar el resto de tiendas
                    .HasMaxLength(50)
                    .HasColumnName("Tienda7");

                entity.Property(e => e.Tienda8) // Agregar el resto de tiendas
                    .HasMaxLength(50)
                    .HasColumnName("Tienda8");

                entity.Property(e => e.Tienda9) // Agregar el resto de tiendas
                    .HasMaxLength(50)
                    .HasColumnName("Tienda9");

                entity.Property(e => e.Tienda10) // Agregar el resto de tiendas
                    .HasMaxLength(50)
                    .HasColumnName("Tienda10");

                entity.Property(e => e.Tienda11) // Agregar el resto de tiendas
                    .HasMaxLength(50)
                    .HasColumnName("Tienda11");

                entity.Property(e => e.Tienda12) // Agregar el resto de tiendas
                    .HasMaxLength(50)
                    .HasColumnName("Tienda12");
                // ... repetir para Tienda2, Tienda3, etc.

                entity.Property(e => e.estado)
                    .HasColumnName("estado");

            });
        }
    }
}
