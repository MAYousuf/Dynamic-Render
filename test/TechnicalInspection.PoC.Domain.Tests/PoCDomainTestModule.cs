using Volo.Abp.Modularity;

namespace TechnicalInspection.PoC;

[DependsOn(
    typeof(PoCDomainModule),
    typeof(PoCTestBaseModule)
)]
public class PoCDomainTestModule : AbpModule
{

}
