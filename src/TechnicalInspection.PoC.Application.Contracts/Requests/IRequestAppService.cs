using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace TechnicalInspection.PoC.Requests;

public interface IRequestAppService : IApplicationService
{
    /// <summary>
    /// Maps a complete posted request into entities, serializes each strongly typed inspection
    /// model into the JSON column, and persists the whole graph in one unit of work.
    /// <para>
    /// This is the only write in the flow: nothing is stored anywhere until it is called.
    /// </para>
    /// </summary>
    Task<Guid> SubmitAsync(CreateInspectionRequestDto input);

    /// <summary>
    /// Reads a submitted request back, rebuilding each inspection's concrete model from the
    /// stored JSON via the type resolver.
    /// </summary>
    Task<InspectionRequestDetailDto?> GetDetailAsync(Guid id);

    Task<List<InspectionRequestListDto>> GetSubmittedListAsync();
}
