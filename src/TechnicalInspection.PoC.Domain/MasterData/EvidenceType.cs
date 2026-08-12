using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace TechnicalInspection.PoC.MasterData;

/// <summary>
/// Fixed master data. Seeded from <c>InspectionDataRegistry</c> so the rows in this table can
/// never describe a combination the application has no C# model for.
/// </summary>
public class EvidenceType : Entity<Guid>
{
    public string Code { get; private set; } = default!;

    public string DisplayName { get; private set; } = default!;

    public int DisplayOrder { get; private set; }

    private EvidenceType()
    {
    }

    public EvidenceType(Guid id, string code, string displayName, int displayOrder)
        : base(id)
    {
        Code = Check.NotNullOrWhiteSpace(code, nameof(code), 64);
        DisplayName = Check.NotNullOrWhiteSpace(displayName, nameof(displayName), 128);
        DisplayOrder = displayOrder;
    }
}
