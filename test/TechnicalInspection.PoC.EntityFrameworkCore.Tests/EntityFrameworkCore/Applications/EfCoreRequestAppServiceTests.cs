using TechnicalInspection.PoC.Requests;
using Xunit;

namespace TechnicalInspection.PoC.EntityFrameworkCore.Applications;

[Collection(PoCTestConsts.CollectionDefinitionName)]
public class EfCoreRequestAppServiceTests : RequestAppServiceTests<PoCEntityFrameworkCoreTestModule>
{
}
