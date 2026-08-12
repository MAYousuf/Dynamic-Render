using Xunit;

namespace TechnicalInspection.PoC.EntityFrameworkCore;

[CollectionDefinition(PoCTestConsts.CollectionDefinitionName)]
public class PoCEntityFrameworkCoreCollection : ICollectionFixture<PoCEntityFrameworkCoreFixture>
{

}
