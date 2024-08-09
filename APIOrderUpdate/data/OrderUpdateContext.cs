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

        }
    }
}
