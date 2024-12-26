using Microsoft.EntityFrameworkCore;

namespace APIOrderUpdate.data
{
    public class OrderUpdateContext : DbContext
    {
        public OrderUpdateContext(DbContextOptions<OrderUpdateContext> options)
            : base(options)
        { 
        }

        public DbSet<OrderCancelEntity> ordenes { get; set; }
        public DbSet<WaveRelease> WaveReleases { get; set; }
        public DbSet<OrdenEnProceso> OrdenEnProceso { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<OrderCancelEntity>(entity =>
            {
                entity.ToTable("Ordenes");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Wave)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.WhId)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.MsgId)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Trandt)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Ordnum)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Schbat)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Cancod)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Accion)  
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<WaveRelease>(entity =>
            {
                entity.ToTable("WaveRelease");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.CodMastr)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.CodInr)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.CantMastr)
                    .IsRequired();

                entity.Property(e => e.CantInr)
                    .IsRequired();

                entity.Property(e => e.Cantidad)
                    .IsRequired();

                entity.Property(e => e.Familia)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.NumOrden)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.CodProducto)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Wave)
                   .IsRequired()
                   .HasMaxLength(50);
            });

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
                    .HasMaxLength(50)
                    .HasColumnName("dtlNumber");

                entity.Property(e => e.subnum)
                    .HasMaxLength(50)
                    .HasColumnName("subnum");

                entity.Property(e => e.estado)
                    .HasColumnName("estado");

                entity.Property(e => e.tienda)
                    .HasMaxLength(50)
                    .HasColumnName("tienda");

                entity.Property(e => e.estadoLuca)
                    .HasColumnName("estadoLuca");
            });

        }
    }
}
