using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace TechnicalInspection.PoC.MasterData;

public class MasterDataAppService : PoCAppService, IMasterDataAppService
{
    private readonly IRepository<EvidenceType, System.Guid> _evidenceTypeRepository;
    private readonly IRepository<InspectionType, System.Guid> _inspectionTypeRepository;
    private readonly IRepository<EvidenceInspectionMapping, System.Guid> _mappingRepository;

    public MasterDataAppService(
        IRepository<EvidenceType, System.Guid> evidenceTypeRepository,
        IRepository<InspectionType, System.Guid> inspectionTypeRepository,
        IRepository<EvidenceInspectionMapping, System.Guid> mappingRepository)
    {
        _evidenceTypeRepository = evidenceTypeRepository;
        _inspectionTypeRepository = inspectionTypeRepository;
        _mappingRepository = mappingRepository;
    }

    public async Task<List<EvidenceTypeDto>> GetEvidenceTypesAsync()
    {
        var types = await _evidenceTypeRepository.GetListAsync();

        return types
            .OrderBy(t => t.DisplayOrder)
            .Select(t => new EvidenceTypeDto { Code = t.Code, DisplayName = t.DisplayName })
            .ToList();
    }

    public async Task<List<InspectionTypeDto>> GetInspectionTypesForEvidenceAsync(string evidenceTypeCode)
    {
        if (string.IsNullOrWhiteSpace(evidenceTypeCode))
        {
            return new List<InspectionTypeDto>();
        }

        var allowedCodes = (await _mappingRepository.GetListAsync(m => m.EvidenceTypeCode == evidenceTypeCode))
            .Select(m => m.InspectionTypeCode)
            .ToHashSet();

        var types = await _inspectionTypeRepository.GetListAsync();

        return types
            .Where(t => allowedCodes.Contains(t.Code))
            .OrderBy(t => t.DisplayOrder)
            .Select(t => new InspectionTypeDto { Code = t.Code, DisplayName = t.DisplayName })
            .ToList();
    }

    public async Task<Dictionary<string, string>> GetInspectionTypeNamesAsync()
    {
        var types = await _inspectionTypeRepository.GetListAsync();
        return types.ToDictionary(t => t.Code, t => t.DisplayName);
    }
}
