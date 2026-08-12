using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace TechnicalInspection.PoC.Requests;

/// <summary>
/// Aggregate root for a technical-inspection request.
/// <para>
/// Named <c>InspectionRequest</c> rather than <c>Request</c> on purpose: Razor's
/// <c>PageModel</c> already exposes a <c>Request</c> property, so the shorter name would be
/// permanently ambiguous inside the pages that use it most.
/// </para>
/// </summary>
public class InspectionRequest : FullAuditedAggregateRoot<Guid>
{
    public string RequestNumber { get; private set; } = default!;

    public string Subject { get; private set; } = default!;

    public DateTime RequestDate { get; private set; }

    public InspectionRequestStatus Status { get; private set; }

    public ICollection<Exhibit> Exhibits { get; private set; }

    private InspectionRequest()
    {
        Exhibits = new List<Exhibit>();
    }

    public InspectionRequest(
        Guid id,
        string requestNumber,
        string subject,
        DateTime requestDate)
        : base(id)
    {
        RequestNumber = Check.NotNullOrWhiteSpace(requestNumber, nameof(requestNumber), 32);
        Subject = Check.NotNullOrWhiteSpace(subject, nameof(subject), 256);
        RequestDate = requestDate;
        Status = InspectionRequestStatus.Draft;
        Exhibits = new List<Exhibit>();
    }

    public Exhibit AddExhibit(Guid id, int sequenceNumber, string? description)
    {
        var exhibit = new Exhibit(id, Id, sequenceNumber, description);
        Exhibits.Add(exhibit);
        return exhibit;
    }

    public void MarkSubmitted()
    {
        if (!Exhibits.Any())
        {
            throw new BusinessException(PoCDomainErrorCodes.RequestHasNoExhibits);
        }

        Status = InspectionRequestStatus.Submitted;
    }
}
