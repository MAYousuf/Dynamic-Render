using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace TechnicalInspection.PoC.Requests;

/// <summary>
/// The stable half of the design: every inspection, whatever its kind, is this one row shape.
/// <para>
/// <see cref="EvidenceTypeCode"/> + <see cref="InspectionTypeCode"/> is the business key that
/// resolves the concrete C# model; <see cref="DataDiscriminator"/> records which model actually
/// produced <see cref="InspectionDataJson"/>, so stale rows can be detected rather than
/// silently mis-deserialized.
/// </para>
/// <para>
/// Adding a new inspection kind adds no column and no table.
/// </para>
/// </summary>
public class Inspection : Entity<Guid>
{
    public Guid EvidenceId { get; private set; }

    public string EvidenceTypeCode { get; private set; } = default!;

    public string InspectionTypeCode { get; private set; } = default!;

    public string DataDiscriminator { get; private set; } = default!;

    /// <summary>
    /// Serialized concrete <c>InspectionData</c>. Stored as nvarchar(max); never queried by
    /// the application, only round-tripped through the serializer.
    /// </summary>
    public string? InspectionDataJson { get; private set; }

    public InspectionDataStatus DataStatus { get; private set; }

    private Inspection()
    {
    }

    internal Inspection(
        Guid id,
        Guid evidenceId,
        string evidenceTypeCode,
        string inspectionTypeCode,
        string dataDiscriminator,
        string? inspectionDataJson,
        InspectionDataStatus dataStatus)
        : base(id)
    {
        EvidenceId = evidenceId;
        EvidenceTypeCode = Check.NotNullOrWhiteSpace(evidenceTypeCode, nameof(evidenceTypeCode), 64);
        InspectionTypeCode = Check.NotNullOrWhiteSpace(inspectionTypeCode, nameof(inspectionTypeCode), 64);
        DataDiscriminator = Check.NotNullOrWhiteSpace(dataDiscriminator, nameof(dataDiscriminator), 64);
        InspectionDataJson = inspectionDataJson;
        DataStatus = dataStatus;
    }

    public void SetData(string dataDiscriminator, string? inspectionDataJson, InspectionDataStatus dataStatus)
    {
        DataDiscriminator = Check.NotNullOrWhiteSpace(dataDiscriminator, nameof(dataDiscriminator), 64);
        InspectionDataJson = inspectionDataJson;
        DataStatus = dataStatus;
    }
}
