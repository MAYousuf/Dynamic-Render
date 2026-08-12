using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace TechnicalInspection.PoC.MasterData;

public class InspectionType : Entity<Guid>
{
    public string Code { get; private set; } = default!;

    public string DisplayName { get; private set; } = default!;

    public int DisplayOrder { get; private set; }

    private InspectionType()
    {
    }

    public InspectionType(Guid id, string code, string displayName, int displayOrder)
        : base(id)
    {
        Code = Check.NotNullOrWhiteSpace(code, nameof(code), 64);
        DisplayName = Check.NotNullOrWhiteSpace(displayName, nameof(displayName), 128);
        DisplayOrder = displayOrder;
    }
}
