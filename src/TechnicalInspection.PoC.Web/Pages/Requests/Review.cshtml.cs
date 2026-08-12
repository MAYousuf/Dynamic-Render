using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TechnicalInspection.PoC.Requests;

namespace TechnicalInspection.PoC.Web.Pages.Requests;

/// <summary>
/// Scenario 6 made visible: reloads a submitted request from SQL Server and shows, for each
/// inspection, the raw JSON column value next to the strongly typed object rebuilt from it.
/// </summary>
public class ReviewModel : PoCPageModel
{
    private readonly IRequestAppService _requestAppService;

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    /// <summary>
    /// Not named <c>Request</c>: that would hide <c>PageModel.Request</c>.
    /// </summary>
    public InspectionRequestDetailDto Detail { get; private set; } = default!;

    public ReviewModel(IRequestAppService requestAppService)
    {
        _requestAppService = requestAppService;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var request = await _requestAppService.GetDetailAsync(Id);

        if (request == null)
        {
            return RedirectToPage("Index");
        }

        Detail = request;
        return Page();
    }
}
