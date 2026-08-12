using TechnicalInspection.PoC.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace TechnicalInspection.PoC.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(PoCEntityFrameworkCoreModule),
    typeof(PoCApplicationContractsModule)
    )]
public class PoCDbMigratorModule : AbpModule
{
}
