using Microsoft.EntityFrameworkCore;

namespace APIWaveRelease.data
{
    public class WaveReleaseContext : DbContext
    {
        public WaveReleaseContext(DbContextOptions<WaveReleaseContext> options)
            : base(options)
        {
        }


        public DbSet<WaveRelease> WaveRelease { get; set; }

        public DbSet<FamilyMaster> FamilyMaster { get; set; }

        public DbSet<WaveReleaseCache> WaveReleaseCache { get; set; }

        public DbSet<WaveActiva> WaveActiva { get; set; }


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

                entity.Property(e => e.tienda) 
                  .HasMaxLength(50);

                entity.Property(e => e.estadoWave)
                    .IsRequired();
            });


       
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
            modelBuilder.Entity<WaveActiva>(entity => {

                entity.ToTable("WaveActiva");
                entity.Property(e => e.Wave);
                entity.Property(e => e.Familia);
                entity.Property(e => e.estado);
            
            });

            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<WaveReleaseCache>(entity =>
            {
                entity.ToTable("WaveReleaseCache");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.WcsId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.WhId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.MsgId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Trandt).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Schbat).HasMaxLength(50);
                entity.Property(e => e.Ordnum).HasMaxLength(50);
                entity.Property(e => e.Cponum).HasMaxLength(50);
                entity.Property(e => e.Rtcust).HasMaxLength(50);
                entity.Property(e => e.Stcust).HasMaxLength(50);
                entity.Property(e => e.Ordtyp).HasMaxLength(50);
                entity.Property(e => e.Adrpsz).HasMaxLength(50);
                entity.Property(e => e.State).HasMaxLength(50);
                entity.Property(e => e.ShipId).HasMaxLength(50);
                entity.Property(e => e.Carcod).HasMaxLength(50);
                entity.Property(e => e.Srvlvl).HasMaxLength(50);
                entity.Property(e => e.Wrkref).HasMaxLength(50);
                entity.Property(e => e.Prtnum).HasMaxLength(50);
                entity.Property(e => e.Prtfam).HasMaxLength(50);
                entity.Property(e => e.AltPrtnum).HasMaxLength(50);
                entity.Property(e => e.MscsEan).HasMaxLength(50);
                entity.Property(e => e.IncsEan).HasMaxLength(50);
                entity.Property(e => e.Stgloc).HasMaxLength(50);
                entity.Property(e => e.MovZoneCode).HasMaxLength(50);
                entity.Property(e => e.Conveyable).HasMaxLength(50);
                entity.Property(e => e.CubicVol).HasColumnType("decimal(18,2)");
            });

        }
    }
}
