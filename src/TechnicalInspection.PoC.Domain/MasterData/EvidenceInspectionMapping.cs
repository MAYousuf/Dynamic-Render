using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace TechnicalInspection.PoC.MasterData;

/// <summary>
/// The allowed (evidence type, inspection type) combinations. Drives the cascading dropdowns in
/// Step 2, so the UI can only ever offer combinations that resolve to a real C# model.
/// </summary>
public class EvidenceInspectionMapping : Entity<Guid>
{
    public string EvidenceTypeCode { get; private set; } = default!;

    public string InspectionTypeCode { get; private set; } = default!;

    private EvidenceInspectionMapping()
    {
    }

    public EvidenceInspectionMapping(Guid id, string evidenceTypeCode, string inspectionTypeCode)
        : base(id)
    {
        EvidenceTypeCode = Check.NotNullOrWhiteSpace(evidenceTypeCode, nameof(evidenceTypeCode), 64);
        InspectionTypeCode = Check.NotNullOrWhiteSpace(inspectionTypeCode, nameof(inspectionTypeCode), 64);
    }
}
