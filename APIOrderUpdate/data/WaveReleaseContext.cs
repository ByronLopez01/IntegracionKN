using Microsoft.EntityFrameworkCore;

namespace APIOrderUpdate.data
{
    public class WaveReleaseContext : DbContext
    {
        public WaveReleaseContext(DbContextOptions<WaveReleaseContext> options)
            : base(options)
        {
        }


        public DbSet<WaveRelease> WaveRelease { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
