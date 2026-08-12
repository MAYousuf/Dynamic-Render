using Microsoft.EntityFrameworkCore;
using TechnicalInspection.PoC.MasterData;
using TechnicalInspection.PoC.Requests;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace TechnicalInspection.PoC.EntityFrameworkCore;

public static class TechnicalInspectionDbContextModelCreatingExtensions
{
    public static void ConfigureTechnicalInspection(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Entity<InspectionRequest>(b =>
        {
            b.ToTable(PoCConsts.DbTablePrefix + "InspectionRequests", PoCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.RequestNumber).IsRequired().HasMaxLength(32);
            b.Property(x => x.Subject).IsRequired().HasMaxLength(256);
            b.Property(x => x.Status).HasConversion<int>();

            b.HasIndex(x => x.RequestNumber);

            b.HasMany(x => x.Exhibits)
                .WithOne()
                .HasForeignKey(x => x.InspectionRequestId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Exhibit>(b =>
        {
            b.ToTable(PoCConsts.DbTablePrefix + "Exhibits", PoCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Description).HasMaxLength(512);

            b.HasIndex(x => x.InspectionRequestId);

            b.HasMany(x => x.Evidences)
                .WithOne()
                .HasForeignKey(x => x.ExhibitId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Evidence>(b =>
        {
            b.ToTable(PoCConsts.DbTablePrefix + "Evidences", PoCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.EvidenceTypeCode).IsRequired().HasMaxLength(64);
            b.Property(x => x.Description).HasMaxLength(512);

            b.HasIndex(x => x.ExhibitId);

            b.HasMany(x => x.Inspections)
                .WithOne()
                .HasForeignKey(x => x.EvidenceId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Inspection>(b =>
        {
            b.ToTable(PoCConsts.DbTablePrefix + "Inspections", PoCConsts.DbSchema);
            b.ConfigureByConvention();

            // The stable columns: the business key that resolves the model, plus the model that
            // actually wrote the payload.
            b.Property(x => x.EvidenceTypeCode).IsRequired().HasMaxLength(64);
            b.Property(x => x.InspectionTypeCode).IsRequired().HasMaxLength(64);
            b.Property(x => x.DataDiscriminator).IsRequired().HasMaxLength(64);
            b.Property(x => x.DataStatus).HasConversion<int>();

            // The variable half of the design. One unbounded text column carries every inspection
            // shape, so adding an inspection kind never alters this table.
            //
            // nvarchar(max) is the correct type on SQL Server 2022; the native `json` type only
            // exists from SQL Server 2025 / Azure SQL onwards. Nothing else here would change.
            b.Property(x => x.InspectionDataJson)
                .HasColumnType("nvarchar(max)");

            b.HasIndex(x => x.EvidenceId);
            b.HasIndex(x => new { x.EvidenceTypeCode, x.InspectionTypeCode });
        });

        builder.Entity<EvidenceType>(b =>
        {
            b.ToTable(PoCConsts.DbTablePrefix + "EvidenceTypes", PoCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(64);
            b.Property(x => x.DisplayName).IsRequired().HasMaxLength(128);

            b.HasIndex(x => x.Code).IsUnique();
        });

        builder.Entity<InspectionType>(b =>
        {
            b.ToTable(PoCConsts.DbTablePrefix + "InspectionTypes", PoCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(64);
            b.Property(x => x.DisplayName).IsRequired().HasMaxLength(128);

            b.HasIndex(x => x.Code).IsUnique();
        });

        builder.Entity<EvidenceInspectionMapping>(b =>
        {
            b.ToTable(PoCConsts.DbTablePrefix + "EvidenceInspectionMappings", PoCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.EvidenceTypeCode).IsRequired().HasMaxLength(64);
            b.Property(x => x.InspectionTypeCode).IsRequired().HasMaxLength(64);

            b.HasIndex(x => new { x.EvidenceTypeCode, x.InspectionTypeCode }).IsUnique();
        });
    }
}
