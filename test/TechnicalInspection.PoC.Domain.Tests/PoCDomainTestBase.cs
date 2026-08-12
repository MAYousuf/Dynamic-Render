using Volo.Abp.Modularity;

namespace TechnicalInspection.PoC;

/* Inherit from this class for your domain layer tests. */
public abstract class PoCDomainTestBase<TStartupModule> : PoCTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
