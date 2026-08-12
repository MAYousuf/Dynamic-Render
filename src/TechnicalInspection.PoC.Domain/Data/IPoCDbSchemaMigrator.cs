using System.Threading.Tasks;

namespace TechnicalInspection.PoC.Data;

public interface IPoCDbSchemaMigrator
{
    Task MigrateAsync();
}
