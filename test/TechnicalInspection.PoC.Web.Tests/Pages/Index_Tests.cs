using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace TechnicalInspection.PoC.Pages;

public class Index_Tests : PoCWebTestBase
{
    [Fact]
    public async Task Welcome_Page()
    {
        var response = await GetResponseAsStringAsync("/");
        response.ShouldNotBeNull();
    }
}
