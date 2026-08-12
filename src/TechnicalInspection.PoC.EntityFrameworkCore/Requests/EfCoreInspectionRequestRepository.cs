using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TechnicalInspection.PoC.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace TechnicalInspection.PoC.Requests;

public class EfCoreInspectionRequestRepository :
    EfCoreRepository<PoCDbContext, InspectionRequest, Guid>,
    IInspectionRequestRepository
{
    public EfCoreInspectionRequestRepository(IDbContextProvider<PoCDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    public async Task<InspectionRequest?> FindWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var dbSet = await GetDbSetAsync();

        return await dbSet
            .Include(r => r.Exhibits)
                .ThenInclude(e => e.Evidences)
                    .ThenInclude(v => v.Inspections)
            .FirstOrDefaultAsync(r => r.Id == id, GetCancellationToken(cancellationToken));
    }

    public override async Task<IQueryable<InspectionRequest>> WithDetailsAsync()
    {
        return (await GetQueryableAsync())
            .Include(r => r.Exhibits)
                .ThenInclude(e => e.Evidences)
                    .ThenInclude(v => v.Inspections);
    }
}
