using System.Linq;
using System.Threading.Tasks;
using TechnicalInspection.PoC.Inspections;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Uow;

namespace TechnicalInspection.PoC.MasterData;

/// <summary>
/// Projects <see cref="IInspectionDataRegistry"/> into the master-data tables.
/// <para>
/// Seeding from the registry (rather than from an independent list) is what guarantees the
/// database can never offer a combination the application has no model for - the concern raised
/// in section 18 of the design about scattering the mapping.
/// </para>
/// </summary>
public class MasterDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<EvidenceType, System.Guid> _evidenceTypeRepository;
    private readonly IRepository<InspectionType, System.Guid> _inspectionTypeRepository;
    private readonly IRepository<EvidenceInspectionMapping, System.Guid> _mappingRepository;
    private readonly IInspectionDataRegistry _registry;
    private readonly IGuidGenerator _guidGenerator;

    public MasterDataSeedContributor(
        IRepository<EvidenceType, System.Guid> evidenceTypeRepository,
        IRepository<InspectionType, System.Guid> inspectionTypeRepository,
        IRepository<EvidenceInspectionMapping, System.Guid> mappingRepository,
        IInspectionDataRegistry registry,
        IGuidGenerator guidGenerator)
    {
        _evidenceTypeRepository = evidenceTypeRepository;
        _inspectionTypeRepository = inspectionTypeRepository;
        _mappingRepository = mappingRepository;
        _registry = registry;
        _guidGenerator = guidGenerator;
    }

    [UnitOfWork]
    public async Task SeedAsync(DataSeedContext context)
    {
        await SeedEvidenceTypesAsync();
        await SeedInspectionTypesAsync();
        await SeedMappingsAsync();
    }

    private async Task SeedEvidenceTypesAsync()
    {
        var existingCodes = (await _evidenceTypeRepository.GetListAsync())
            .Select(e => e.Code)
            .ToHashSet();

        var missing = _registry.EvidenceTypes
            .Where(d => !existingCodes.Contains(d.Code))
            .Select(d => new EvidenceType(_guidGenerator.Create(), d.Code, d.DisplayName, d.DisplayOrder))
            .ToList();

        if (missing.Any())
        {
            await _evidenceTypeRepository.InsertManyAsync(missing, autoSave: true);
        }
    }

    private async Task SeedInspectionTypesAsync()
    {
        var existingCodes = (await _inspectionTypeRepository.GetListAsync())
            .Select(e => e.Code)
            .ToHashSet();

        var missing = _registry.InspectionTypes
            .Where(d => !existingCodes.Contains(d.Code))
            .Select(d => new InspectionType(_guidGenerator.Create(), d.Code, d.DisplayName, d.DisplayOrder))
            .ToList();

        if (missing.Any())
        {
            await _inspectionTypeRepository.InsertManyAsync(missing, autoSave: true);
        }
    }

    private async Task SeedMappingsAsync()
    {
        var existing = (await _mappingRepository.GetListAsync())
            .Select(m => (m.EvidenceTypeCode, m.InspectionTypeCode))
            .ToHashSet();

        var missing = _registry.Definitions
            .Select(d => (d.EvidenceTypeCode, d.InspectionTypeCode))
            .Distinct()
            .Where(c => !existing.Contains(c))
            .Select(c => new EvidenceInspectionMapping(
                _guidGenerator.Create(),
                c.EvidenceTypeCode,
                c.InspectionTypeCode))
            .ToList();

        if (missing.Any())
        {
            await _mappingRepository.InsertManyAsync(missing, autoSave: true);
        }
    }
}
