using Volo.Abp.Modularity;

namespace TechnicalInspection.PoC;

[DependsOn(
    typeof(PoCApplicationModule),
    typeof(PoCDomainTestModule)
)]
public class PoCApplicationTestModule : AbpModule
{

}
