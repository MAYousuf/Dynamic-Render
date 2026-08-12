using System;
using System.Collections.Generic;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace TechnicalInspection.PoC.Requests;

public class Evidence : Entity<Guid>
{
    public Guid ExhibitId { get; private set; }

    /// <summary>
    /// Master-data code (not an enum) so new evidence types are data, not a recompile.
    /// </summary>
    public string EvidenceTypeCode { get; private set; } = default!;

    public string? Description { get; private set; }

    public ICollection<Inspection> Inspections { get; private set; }

    private Evidence()
    {
        Inspections = new List<Inspection>();
    }

    internal Evidence(Guid id, Guid exhibitId, string evidenceTypeCode, string? description)
        : base(id)
    {
        ExhibitId = exhibitId;
        EvidenceTypeCode = Check.NotNullOrWhiteSpace(evidenceTypeCode, nameof(evidenceTypeCode), 64);
        Description = description;
        Inspections = new List<Inspection>();
    }

    public Inspection AddInspection(
        Guid id,
        string inspectionTypeCode,
        string dataDiscriminator,
        string? inspectionDataJson,
        InspectionDataStatus status)
    {
        var inspection = new Inspection(
            id,
            Id,
            EvidenceTypeCode,
            inspectionTypeCode,
            dataDiscriminator,
            inspectionDataJson,
            status);

        Inspections.Add(inspection);
        return inspection;
    }
}
