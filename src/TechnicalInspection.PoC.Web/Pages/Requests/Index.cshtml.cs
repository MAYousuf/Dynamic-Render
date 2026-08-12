using System.Collections.Generic;
using System.Threading.Tasks;
using TechnicalInspection.PoC.Requests;

namespace TechnicalInspection.PoC.Web.Pages.Requests;

/// <summary>
/// Entry point for the demo. Everything listed here is in the database: the request form holds its
/// state in one form until it is submitted, so there is no in-progress state to show.
/// </summary>
public class IndexModel : PoCPageModel
{
    private readonly IRequestAppService _requestAppService;

    public List<InspectionRequestListDto> Submitted { get; private set; } = new();

    public IndexModel(IRequestAppService requestAppService)
    {
        _requestAppService = requestAppService;
    }

    public async Task OnGetAsync()
    {
        Submitted = await _requestAppService.GetSubmittedListAsync();
    }
}
