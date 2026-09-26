using ITPQuotation.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ITPQuotation.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Rfq> Rfqs => Set<Rfq>();
    public DbSet<RfqItem> RfqItems => Set<RfqItem>();
    public DbSet<MaterialMaster> MaterialMasters => Set<MaterialMaster>();
    public DbSet<MetalMaterial> MetalMaterials => Set<MetalMaterial>();
    public DbSet<ProcessMaster> ProcessMasters => Set<ProcessMaster>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<VendorProcessRate> VendorProcessRates => Set<VendorProcessRate>();
    public DbSet<MaterialRouting> MaterialRoutings => Set<MaterialRouting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Rfq>()
            .HasOne<Customer>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<RfqItem>()
            .HasOne<Rfq>()
            .WithMany()
            .HasForeignKey(x => x.RfqId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Customer>()
            .Property(x => x.CustomerName)
            .HasMaxLength(200)
            .IsRequired();
        modelBuilder.Entity<Rfq>()
            .Property(x => x.RfqNumber)
            .HasMaxLength(100)
            .IsRequired();
        modelBuilder.Entity<RfqItem>()
            .Property(x => x.MaterialNo)
            .HasMaxLength(100)
            .IsRequired();
        modelBuilder.Entity<RfqItem>()
            .Property(x => x.Quantity)
            .HasPrecision(18, 3);

        modelBuilder.Entity<MaterialMaster>(entity =>
        {
            entity.Property(x => x.MaterialNo).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ShortDescription).HasMaxLength(500);
            entity.Property(x => x.Grade).HasMaxLength(100);
            entity.Property(x => x.DrawingCode).HasMaxLength(200);
            entity.Property(x => x.DrawingVersion).HasMaxLength(50);
            entity.Property(x => x.FinishSize).HasMaxLength(200);
            entity.Property(x => x.RawMaterialShape).HasMaxLength(50);
            entity.Property(x => x.RawMaterialSize).HasMaxLength(200);
            entity.Property(x => x.Notes).HasMaxLength(4000);
            entity.HasIndex(x => x.MaterialNo).IsUnique();
            entity.HasOne(x => x.MetalMaterial)
                .WithMany(x => x.Materials)
                .HasForeignKey(x => x.MetalMaterialId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MetalMaterial>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Grade).HasMaxLength(100);
            entity.Property(x => x.DensityKgM3).HasPrecision(18, 6);
            entity.Property(x => x.DefaultRatePerKg).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.Name, x.Grade });
        });

        modelBuilder.Entity<ProcessMaster>(entity =>
        {
            entity.Property(x => x.ProcessName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(4000);
            entity.Property(x => x.DefaultProcessType).HasMaxLength(20).IsRequired();
            entity.Property(x => x.DefaultMachineRate).HasPrecision(18, 2);
            entity.HasIndex(x => x.ProcessName);
        });

        modelBuilder.Entity<Vendor>(entity =>
        {
            entity.Property(x => x.VendorName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ContactPerson).HasMaxLength(200);
            entity.Property(x => x.Phone).HasMaxLength(50);
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.Property(x => x.Address).HasMaxLength(1000);
            entity.HasIndex(x => x.VendorName);
        });

        modelBuilder.Entity<VendorProcessRate>(entity =>
        {
            entity.Property(x => x.RateType).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Rate).HasPrecision(18, 2);
            entity.Property(x => x.Notes).HasMaxLength(4000);
            entity.HasIndex(x => new { x.VendorId, x.ProcessId, x.EffectiveFrom });
            entity.HasOne(x => x.Vendor)
                .WithMany(x => x.ProcessRates)
                .HasForeignKey(x => x.VendorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Process)
                .WithMany(x => x.VendorProcessRates)
                .HasForeignKey(x => x.ProcessId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MaterialRouting>(entity =>
        {
            entity.Property(x => x.ProcessType).HasMaxLength(20).IsRequired();
            entity.Property(x => x.MachineRate).HasPrecision(18, 2);
            entity.Property(x => x.SetupTimeHours).HasPrecision(18, 4);
            entity.Property(x => x.CycleTimeHours).HasPrecision(18, 4);
            entity.Property(x => x.RatePerPiece).HasPrecision(18, 2);
            entity.Property(x => x.Notes).HasMaxLength(4000);
            entity.HasIndex(x => new { x.MaterialMasterId, x.Sequence }).IsUnique();
            entity.HasOne(x => x.MaterialMaster)
                .WithMany(x => x.Routings)
                .HasForeignKey(x => x.MaterialMasterId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Process)
                .WithMany(x => x.MaterialRoutings)
                .HasForeignKey(x => x.ProcessId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Vendor)
                .WithMany(x => x.MaterialRoutings)
                .HasForeignKey(x => x.VendorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
