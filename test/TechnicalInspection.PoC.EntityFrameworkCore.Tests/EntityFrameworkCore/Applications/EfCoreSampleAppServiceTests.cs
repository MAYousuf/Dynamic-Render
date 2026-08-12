using TechnicalInspection.PoC.Samples;
using Xunit;

namespace TechnicalInspection.PoC.EntityFrameworkCore.Applications;

[Collection(PoCTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<PoCEntityFrameworkCoreTestModule>
{

}
