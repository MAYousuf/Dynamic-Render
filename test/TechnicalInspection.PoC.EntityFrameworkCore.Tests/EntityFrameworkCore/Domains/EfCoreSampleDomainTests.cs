using TechnicalInspection.PoC.Samples;
using Xunit;

namespace TechnicalInspection.PoC.EntityFrameworkCore.Domains;

[Collection(PoCTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<PoCEntityFrameworkCoreTestModule>
{

}
