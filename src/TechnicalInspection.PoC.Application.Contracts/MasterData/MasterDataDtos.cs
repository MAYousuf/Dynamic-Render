using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace TechnicalInspection.PoC.MasterData;

public class EvidenceTypeDto
{
    public string Code { get; set; } = default!;

    public string DisplayName { get; set; } = default!;
}

public class InspectionTypeDto
{
    public string Code { get; set; } = default!;

    public string DisplayName { get; set; } = default!;
}

public interface IMasterDataAppService : IApplicationService
{
    Task<List<EvidenceTypeDto>> GetEvidenceTypesAsync();

    /// <summary>
    /// The inspection types permitted for an evidence type, read from the seeded mapping table so
    /// the UI can only offer combinations that resolve to a real model.
    /// </summary>
    Task<List<InspectionTypeDto>> GetInspectionTypesForEvidenceAsync(string evidenceTypeCode);

    Task<Dictionary<string, string>> GetInspectionTypeNamesAsync();
}
