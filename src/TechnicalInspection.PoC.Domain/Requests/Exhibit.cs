using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities;

namespace TechnicalInspection.PoC.Requests;

public class Exhibit : Entity<Guid>
{
    public Guid InspectionRequestId { get; private set; }

    public int SequenceNumber { get; private set; }

    public string? Description { get; private set; }

    public ICollection<Evidence> Evidences { get; private set; }

    private Exhibit()
    {
        Evidences = new List<Evidence>();
    }

    internal Exhibit(Guid id, Guid inspectionRequestId, int sequenceNumber, string? description)
        : base(id)
    {
        InspectionRequestId = inspectionRequestId;
        SequenceNumber = sequenceNumber;
        Description = description;
        Evidences = new List<Evidence>();
    }

    public Evidence AddEvidence(Guid id, string evidenceTypeCode, string? description)
    {
        var evidence = new Evidence(id, Id, evidenceTypeCode, description);
        Evidences.Add(evidence);
        return evidence;
    }
}
