using Microsoft.AspNetCore.Builder;
using TechnicalInspection.PoC;
using Volo.Abp.AspNetCore.TestBase;

var builder = WebApplication.CreateBuilder();

builder.Environment.ContentRootPath = GetWebProjectContentRootPathHelper.Get("TechnicalInspection.PoC.Web.csproj");
await builder.RunAbpModuleAsync<PoCWebTestModule>(applicationName: "TechnicalInspection.PoC.Web" );

public partial class Program
{
}
