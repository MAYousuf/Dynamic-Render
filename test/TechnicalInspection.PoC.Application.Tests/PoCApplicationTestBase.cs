using Volo.Abp.Modularity;

namespace TechnicalInspection.PoC;

public abstract class PoCApplicationTestBase<TStartupModule> : PoCTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
