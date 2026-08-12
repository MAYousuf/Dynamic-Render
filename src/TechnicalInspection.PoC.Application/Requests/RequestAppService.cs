using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TechnicalInspection.PoC.Inspections;
using TechnicalInspection.PoC.MasterData;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace TechnicalInspection.PoC.Requests;

public class RequestAppService : PoCAppService, IRequestAppService
{
    private readonly IInspectionRequestRepository _requestRepository;
    private readonly IRepository<EvidenceType, Guid> _evidenceTypeRepository;
    private readonly IRepository<InspectionType, Guid> _inspectionTypeRepository;
    private readonly IInspectionDataTypeResolver _resolver;
    private readonly IInspectionDataSerializer _serializer;

    public RequestAppService(
        IInspectionRequestRepository requestRepository,
        IRepository<EvidenceType, Guid> evidenceTypeRepository,
        IRepository<InspectionType, Guid> inspectionTypeRepository,
        IInspectionDataTypeResolver resolver,
        IInspectionDataSerializer serializer)
    {
        _requestRepository = requestRepository;
        _evidenceTypeRepository = evidenceTypeRepository;
        _inspectionTypeRepository = inspectionTypeRepository;
        _resolver = resolver;
        _serializer = serializer;
    }

    /// <summary>
    /// The write half of the design, and the only persistence point in the whole flow: strongly
    /// typed derived models in, one JSON column out, one unit of work.
    /// </summary>
    [UnitOfWork]
    public async Task<Guid> SubmitAsync(CreateInspectionRequestDto input)
    {
        Check.NotNull(input, nameof(input));

        if (!input.Exhibits.Any())
        {
            throw new BusinessException(PoCDomainErrorCodes.RequestHasNoExhibits);
        }

        var request = new InspectionRequest(
            GuidGenerator.Create(),
            input.RequestNumber,
            input.Subject,
            input.RequestDate);

        foreach (var exhibitInput in input.Exhibits)
        {
            if (!exhibitInput.Evidences.Any())
            {
                throw new BusinessException(PoCDomainErrorCodes.RequestIncomplete)
                    .WithData("Exhibit", exhibitInput.SequenceNumber);
            }

            var exhibit = request.AddExhibit(
                GuidGenerator.Create(),
                exhibitInput.SequenceNumber,
                exhibitInput.Description);

            foreach (var evidenceInput in exhibitInput.Evidences)
            {
                if (!evidenceInput.Inspections.Any())
                {
                    throw new BusinessException(PoCDomainErrorCodes.RequestIncomplete)
                        .WithData("Exhibit", exhibitInput.SequenceNumber)
                        .WithData("EvidenceTypeCode", evidenceInput.EvidenceTypeCode);
                }

                var evidence = exhibit.AddEvidence(
                    GuidGenerator.Create(),
                    evidenceInput.EvidenceTypeCode,
                    evidenceInput.Description);

                foreach (var inspectionInput in evidenceInput.Inspections)
                {
                    // Resolving again here (rather than trusting what was posted) means a
                    // combination that is not supported fails loudly instead of persisting data
                    // nothing can read back.
                    var definition = _resolver.Resolve(
                        evidenceInput.EvidenceTypeCode,
                        inspectionInput.InspectionTypeCode);

                    var data = inspectionInput.Data
                               ?? throw new BusinessException(PoCDomainErrorCodes.InspectionDataMissing)
                                   .WithData("EvidenceTypeCode", evidenceInput.EvidenceTypeCode)
                                   .WithData("InspectionTypeCode", inspectionInput.InspectionTypeCode);

                    if (data.GetType() != definition.DataType)
                    {
                        throw new BusinessException(PoCDomainErrorCodes.InspectionDataTypeMismatch)
                            .WithData("ExpectedType", definition.DataType.Name)
                            .WithData("ActualType", data.GetType().Name);
                    }

                    evidence.AddInspection(
                        GuidGenerator.Create(),
                        inspectionInput.InspectionTypeCode,
                        definition.Discriminator,
                        _serializer.Serialize(data),
                        InspectionDataStatus.Completed);
                }
            }
        }

        request.MarkSubmitted();

        await _requestRepository.InsertAsync(request, autoSave: true);

        return request.Id;
    }

    /// <summary>
    /// The read half: one column in, a different concrete type per row out.
    /// </summary>
    public async Task<InspectionRequestDetailDto?> GetDetailAsync(Guid id)
    {
        var request = await _requestRepository.FindWithDetailsAsync(id);

        if (request == null)
        {
            return null;
        }

        var evidenceTypeNames = (await _evidenceTypeRepository.GetListAsync())
            .ToDictionary(t => t.Code, t => t.DisplayName);

        var inspectionTypeNames = (await _inspectionTypeRepository.GetListAsync())
            .ToDictionary(t => t.Code, t => t.DisplayName);

        return new InspectionRequestDetailDto
        {
            Id = request.Id,
            RequestNumber = request.RequestNumber,
            Subject = request.Subject,
            RequestDate = request.RequestDate,
            Status = request.Status,
            Exhibits = request.Exhibits
                .OrderBy(e => e.SequenceNumber)
                .Select(e => new ExhibitDto
                {
                    Id = e.Id,
                    SequenceNumber = e.SequenceNumber,
                    Description = e.Description,
                    Evidences = e.Evidences
                        .Select(v => new EvidenceDto
                        {
                            Id = v.Id,
                            EvidenceTypeCode = v.EvidenceTypeCode,
                            EvidenceTypeDisplayName = evidenceTypeNames.GetOrDefault(v.EvidenceTypeCode)
                                                     ?? v.EvidenceTypeCode,
                            Description = v.Description,
                            Inspections = v.Inspections
                                .Select(i => MapInspection(i, inspectionTypeNames))
                                .ToList()
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    private InspectionDto MapInspection(Inspection inspection, Dictionary<string, string> inspectionTypeNames)
    {
        var dto = new InspectionDto
        {
            Id = inspection.Id,
            EvidenceTypeCode = inspection.EvidenceTypeCode,
            InspectionTypeCode = inspection.InspectionTypeCode,
            InspectionTypeDisplayName = inspectionTypeNames.GetOrDefault(inspection.InspectionTypeCode)
                                        ?? inspection.InspectionTypeCode,
            DataDiscriminator = inspection.DataDiscriminator,
            DataStatus = inspection.DataStatus,
            RawJson = inspection.InspectionDataJson ?? string.Empty
        };

        if (string.IsNullOrWhiteSpace(inspection.InspectionDataJson))
        {
            return dto;
        }

        dto.FormattedJson = _serializer.Prettify(inspection.InspectionDataJson);

        try
        {
            // Resolve the expected type from the business combination, then require the stored
            // payload to actually be that type. This is the section 11 read path.
            var definition = _resolver.Resolve(
                inspection.EvidenceTypeCode,
                inspection.InspectionTypeCode);

            dto.Data = _serializer.DeserializeAs(inspection.InspectionDataJson, definition.DataType);
            dto.DataTypeName = dto.Data.GetType().Name;
        }
        catch (BusinessException ex)
        {
            // A row written by a model that no longer matches its combination is surfaced rather
            // than silently rendered as blank.
            dto.DeserializationError = ex.Code;
        }

        return dto;
    }

    public async Task<List<InspectionRequestListDto>> GetSubmittedListAsync()
    {
        var queryable = await _requestRepository.WithDetailsAsync();

        var requests = await AsyncExecuter.ToListAsync(
            queryable
                .Where(r => r.Status == InspectionRequestStatus.Submitted)
                .OrderByDescending(r => r.CreationTime));

        return requests
            .Select(r => new InspectionRequestListDto
            {
                Id = r.Id,
                RequestNumber = r.RequestNumber,
                Subject = r.Subject,
                RequestDate = r.RequestDate,
                CreationTime = r.CreationTime,
                InspectionCount = r.Exhibits
                    .SelectMany(e => e.Evidences)
                    .SelectMany(v => v.Inspections)
                    .Count()
            })
            .ToList();
    }
}
