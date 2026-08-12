using System;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace TechnicalInspection.PoC.Requests;

public interface IInspectionRequestRepository : IRepository<InspectionRequest, Guid>
{
    /// <summary>
    /// Loads the full Exhibit -> Evidence -> Inspection graph in one round trip.
    /// </summary>
    Task<InspectionRequest?> FindWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
