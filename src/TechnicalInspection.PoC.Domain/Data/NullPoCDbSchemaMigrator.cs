using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace TechnicalInspection.PoC.Data;

/* This is used if database provider does't define
 * IPoCDbSchemaMigrator implementation.
 */
public class NullPoCDbSchemaMigrator : IPoCDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
