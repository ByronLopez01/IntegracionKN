﻿using Microsoft.EntityFrameworkCore;

namespace APIOrderConfirmation.data
{
    public class OrderConfirmationContext : DbContext
    {
        public OrderConfirmationContext(DbContextOptions<OrderConfirmationContext> options)
            : base(options)
        {
        }
        public DbSet<OrdenEnProceso> ordenesEnProceso { get; set; }
        public DbSet<Ordenes> ordenes { get; set; }
        public DbSet<Confirmada> Confirmada { get; set; }


        public DbSet<WaveRelease> WaveRelease { get; set; }

        public DbSet<FamilyMaster> FamilyMaster { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //configuracion para tabla ordenes
            modelBuilder.Entity<Ordenes>(entity =>
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

                entity.Property(e => e.estadoLuca)
                    .HasColumnName("estadoLuca");

                entity.Property(e => e.fechaCreacion)
                    .HasColumnName("fechaCreacion");

                entity.Property(e => e.fechaProceso)
                    .HasColumnName("fechaProceso");
            });

            modelBuilder.Entity<Confirmada>(entity =>
            {
                entity.ToTable("Confirmada");

                entity.Property(e => e.WcsId).HasMaxLength(50).IsRequired();
                entity.Property(e => e.WhId).HasMaxLength(50).IsRequired();
                entity.Property(e => e.MsgId).HasMaxLength(50).IsRequired();
                entity.Property(e => e.TranDt).IsRequired();
                entity.Property(e => e.LodNum).HasMaxLength(50).IsRequired();
                entity.Property(e => e.SubNum).HasMaxLength(50).IsRequired();
                entity.Property(e => e.DtlNum).HasMaxLength(50).IsRequired();
                entity.Property(e => e.StoLoc).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Qty).IsRequired();
                entity.Property(e => e.accion).HasMaxLength(50);
            });

            modelBuilder.Entity<FamilyMaster>(entity =>
            {
                entity.ToTable("FamilyMaster");

                entity.HasKey(e => e.IdFamilyMaster);

                entity.Property(e => e.Familia)
                    .HasColumnName("familia")
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.NumSalida)
                    .HasColumnName("numSalida")
                    .IsRequired();

                entity.Property(e => e.NumTanda)
                    .HasColumnName("numTanda")
                    .IsRequired();

                entity.Property(e => e.estado)
                    .HasColumnName("estado")
                    .IsRequired();
            });

            modelBuilder.Entity<WaveRelease>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Wave)
                    .HasColumnName("Wave")
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.estadoWave)
                    .HasColumnName("estadoWave")
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.familia)
                    .HasColumnName("Familia")
                    .IsRequired()
                    .HasMaxLength(50);


                

            });
        }

    }
}
