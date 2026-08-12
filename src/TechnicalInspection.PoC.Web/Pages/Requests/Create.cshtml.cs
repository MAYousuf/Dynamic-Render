using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TechnicalInspection.PoC.Inspections;
using TechnicalInspection.PoC.MasterData;
using TechnicalInspection.PoC.Requests;
using Volo.Abp;

namespace TechnicalInspection.PoC.Web.Pages.Requests;

/// <summary>
/// The whole request wizard: one page, one form, one post.
/// <para>
/// The three "steps" are panes toggled in the browser, and exhibits, evidences, inspections and
/// their data cards are added and removed client-side by cloning server-rendered templates. Nothing
/// is stored anywhere until <see cref="OnPostAsync"/> succeeds, which is the only write in the flow.
/// </para>
/// <para>
/// What did not change is the part the PoC is about: the data card for each inspection is still a
/// strongly typed Razor partial resolved server-side from its (evidence type, inspection type)
/// combination, and every card in the post is still bound to its concrete
/// <see cref="InspectionData"/> subclass by <c>InspectionDataModelBinder</c>.
/// </para>
/// </summary>
public class CreateModel : PoCPageModel
{
    /// <summary>Sentinel prefixes the templates are rendered under; rewritten by Create.js on clone.</summary>
    public const string ExhibitTemplatePrefix = "ExhibitTemplate[0]";

    public const string DataTemplatePrefix = "DataTemplates";

    private readonly IMasterDataAppService _masterDataAppService;
    private readonly IInspectionDataRegistry _registry;
    private readonly IInspectionDataTypeResolver _resolver;
    private readonly IRequestAppService _requestAppService;

    [BindProperty]
    public BasicDataInput Basic { get; set; } = new();

    [BindProperty]
    public List<ExhibitInput> Exhibits { get; set; } = new();

    /// <summary>
    /// One entry per configured inspection, flat rather than nested so a card can be moved around
    /// in the browser without disturbing the structure it came from.
    /// </summary>
    [BindProperty]
    public List<InspectionEntryInput> Inspections { get; set; } = new();

    /// <summary>Dummy graph that exists only to give the row templates something to render against.</summary>
    public List<ExhibitInput> ExhibitTemplate { get; } = new()
    {
        new ExhibitInput
        {
            Evidences = { new EvidenceInput { Inspections = { new InspectionInput() } } }
        }
    };

    /// <summary>One entry per supported combination, each pre-instantiated to its concrete type.</summary>
    public List<InspectionEntryInput> DataTemplates { get; private set; } = new();

    public List<InspectionCardContext> DataTemplateContexts { get; private set; } = new();

    /// <summary>Context per inspection id, rebuilt from the posted structure on redisplay.</summary>
    public Dictionary<Guid, InspectionCardContext> Contexts { get; private set; } = new();

    public List<SelectListItem> EvidenceTypeOptions { get; private set; } = new();

    public Dictionary<string, List<SelectListItem>> InspectionTypeOptions { get; private set; } = new();

    /// <summary>Read by Create.js so a failed post lands the user on the pane that has errors.</summary>
    public int ActivePane { get; private set; } = 1;

    public string ClientConfigJson { get; private set; } = "{}";

    public CreateModel(
        IMasterDataAppService masterDataAppService,
        IInspectionDataRegistry registry,
        IInspectionDataTypeResolver resolver,
        IRequestAppService requestAppService)
    {
        _masterDataAppService = masterDataAppService;
        _registry = registry;
        _resolver = resolver;
        _requestAppService = requestAppService;
    }

    public async Task OnGetAsync()
    {
        Basic = new BasicDataInput
        {
            RequestNumber = $"REQ-{DateTime.Now:yyyyMMdd-HHmmss}",
            RequestDate = DateTime.Today
        };

        // One empty exhibit so the form is usable immediately; everything after this is added
        // client-side.
        Exhibits = new List<ExhibitInput>
        {
            new()
            {
                SequenceNumber = 1,
                Evidences = { new EvidenceInput() }
            }
        };

        await PrepareViewAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await PrepareViewAsync();

        ValidateStructure();
        MatchDataCardsToStructure();

        if (!ModelState.IsValid)
        {
            ActivePane = DetermineFailingPane();
            return Page();
        }

        try
        {
            var requestId = await _requestAppService.SubmitAsync(BuildInput());
            return RedirectToPage("Review", new { id = requestId });
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, $"Submit failed: {ex.Code}");
            ActivePane = 3;
            return Page();
        }
    }

    /// <summary>
    /// The same structural rules the old Step 2 enforced. Create.js checks these before it lets the
    /// user reach pane 3, but that check is convenience only - this one decides.
    /// </summary>
    private void ValidateStructure()
    {
        var problems = new List<string>();

        if (!Exhibits.Any())
        {
            problems.Add("Add at least one exhibit.");
        }

        foreach (var exhibit in Exhibits)
        {
            if (!exhibit.Evidences.Any())
            {
                problems.Add($"Exhibit #{exhibit.SequenceNumber}: add at least one evidence.");
            }

            foreach (var evidence in exhibit.Evidences)
            {
                if (string.IsNullOrWhiteSpace(evidence.EvidenceTypeCode))
                {
                    problems.Add($"Exhibit #{exhibit.SequenceNumber}: every evidence needs an evidence type.");
                    continue;
                }

                if (!evidence.Inspections.Any())
                {
                    problems.Add($"Exhibit #{exhibit.SequenceNumber}: evidence needs at least one inspection.");
                }

                if (evidence.Inspections.Any(i => string.IsNullOrWhiteSpace(i.InspectionTypeCode)))
                {
                    problems.Add($"Exhibit #{exhibit.SequenceNumber}: every inspection needs an inspection type.");
                }
            }
        }

        if (!Exhibits.SelectMany(e => e.Evidences).SelectMany(v => v.Inspections).Any())
        {
            problems.Add("Add at least one inspection.");
        }

        foreach (var problem in problems.Distinct())
        {
            ModelState.AddModelError(string.Empty, problem);
        }
    }

    /// <summary>
    /// Every configured inspection must have posted exactly one data card, and that card's
    /// discriminator must match what the combination actually resolves to. The discriminator arrives
    /// as a hidden field, so without this check a tampered field could bind a different model than
    /// the request is configured for.
    /// </summary>
    private void MatchDataCardsToStructure()
    {
        var matched = new HashSet<Guid>();

        foreach (var (exhibit, evidence, inspection) in EnumerateStructure())
        {
            if (string.IsNullOrWhiteSpace(evidence.EvidenceTypeCode) ||
                string.IsNullOrWhiteSpace(inspection.InspectionTypeCode))
            {
                // Already reported by ValidateStructure.
                continue;
            }

            if (!_resolver.TryResolve(evidence.EvidenceTypeCode, inspection.InspectionTypeCode, out var definition))
            {
                ModelState.AddModelError(string.Empty,
                    $"Exhibit #{exhibit.SequenceNumber}: '{evidence.EvidenceTypeCode}' / " +
                    $"'{inspection.InspectionTypeCode}' is not a supported inspection combination.");
                continue;
            }

            var index = Inspections.FindIndex(e => e.InspectionId == inspection.Id);

            if (index < 0)
            {
                ModelState.AddModelError(string.Empty,
                    $"Exhibit #{exhibit.SequenceNumber}: no data was submitted for the " +
                    $"{definition.InspectionTypeCode} inspection.");
                continue;
            }

            if (Inspections[index].Discriminator != definition.Discriminator)
            {
                ModelState.AddModelError(
                    $"Inspections[{index}].Discriminator",
                    "The submitted inspection kind does not match this request's configuration.");
            }

            matched.Add(inspection.Id);
        }

        if (Inspections.Any(e => !matched.Contains(e.InspectionId)))
        {
            ModelState.AddModelError(string.Empty,
                "Inspection data was submitted for an inspection that is no longer part of this request.");
        }
    }

    private CreateInspectionRequestDto BuildInput()
    {
        var dataByInspectionId = Inspections.ToDictionary(e => e.InspectionId, e => e.Data);

        return new CreateInspectionRequestDto
        {
            RequestNumber = Basic.RequestNumber!,
            Subject = Basic.Subject!,
            RequestDate = Basic.RequestDate,
            Exhibits = Exhibits
                .Select((exhibit, index) => new CreateExhibitDto
                {
                    SequenceNumber = index + 1,
                    Description = exhibit.Description,
                    Evidences = exhibit.Evidences
                        .Select(evidence => new CreateEvidenceDto
                        {
                            EvidenceTypeCode = evidence.EvidenceTypeCode!,
                            Description = evidence.Description,
                            Inspections = evidence.Inspections
                                .Select(inspection => new CreateInspectionDto
                                {
                                    InspectionTypeCode = inspection.InspectionTypeCode!,
                                    Data = dataByInspectionId[inspection.Id]!
                                })
                                .ToList()
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    /// <summary>
    /// Errors on Basic.* belong to pane 1, errors on Exhibits.* to pane 2, and everything else -
    /// inspection data and model-level problems - to pane 3.
    /// </summary>
    private int DetermineFailingPane()
    {
        if (ModelState.Keys.Any(k => k.StartsWith("Basic.", StringComparison.Ordinal) &&
                                     ModelState[k]!.Errors.Any()))
        {
            return 1;
        }

        if (ModelState.Keys.Any(k => k.StartsWith("Exhibits[", StringComparison.Ordinal) &&
                                     ModelState[k]!.Errors.Any()))
        {
            return 2;
        }

        return 3;
    }

    private IEnumerable<(ExhibitInput Exhibit, EvidenceInput Evidence, InspectionInput Inspection)> EnumerateStructure()
    {
        foreach (var exhibit in Exhibits)
        {
            foreach (var evidence in exhibit.Evidences)
            {
                foreach (var inspection in evidence.Inspections)
                {
                    yield return (exhibit, evidence, inspection);
                }
            }
        }
    }

    private async Task PrepareViewAsync()
    {
        await LoadMasterDataAsync();
        BuildDataTemplates();
        await BuildContextsAsync();
        BuildClientConfig();
    }

    private async Task LoadMasterDataAsync()
    {
        var evidenceTypes = await _masterDataAppService.GetEvidenceTypesAsync();

        EvidenceTypeOptions = evidenceTypes
            .Select(t => new SelectListItem(t.DisplayName, t.Code))
            .ToList();

        foreach (var evidenceType in evidenceTypes)
        {
            var inspectionTypes = await _masterDataAppService
                .GetInspectionTypesForEvidenceAsync(evidenceType.Code);

            InspectionTypeOptions[evidenceType.Code] = inspectionTypes
                .Select(t => new SelectListItem(t.DisplayName, t.Code))
                .ToList();
        }
    }

    /// <summary>
    /// One template per supported combination. <c>Data</c> is instantiated so the strongly typed
    /// partial renders against a real object of its own model type rather than null.
    /// </summary>
    private void BuildDataTemplates()
    {
        DataTemplates = new List<InspectionEntryInput>();
        DataTemplateContexts = new List<InspectionCardContext>();

        foreach (var definition in _registry.Definitions)
        {
            DataTemplates.Add(new InspectionEntryInput
            {
                Discriminator = definition.Discriminator,
                Data = (InspectionData)Activator.CreateInstance(definition.DataType)!
            });

            DataTemplateContexts.Add(new InspectionCardContext
            {
                Discriminator = definition.Discriminator,
                PartialViewName = definition.PartialViewName,
                DataTypeName = definition.DataType.Name
            });
        }
    }

    private async Task BuildContextsAsync()
    {
        Contexts = new Dictionary<Guid, InspectionCardContext>();

        if (!Inspections.Any())
        {
            return;
        }

        var evidenceTypeNames = (await _masterDataAppService.GetEvidenceTypesAsync())
            .ToDictionary(t => t.Code, t => t.DisplayName);

        var inspectionTypeNames = await _masterDataAppService.GetInspectionTypeNamesAsync();

        foreach (var (exhibit, evidence, inspection) in EnumerateStructure())
        {
            if (string.IsNullOrWhiteSpace(evidence.EvidenceTypeCode) ||
                string.IsNullOrWhiteSpace(inspection.InspectionTypeCode) ||
                !_resolver.TryResolve(evidence.EvidenceTypeCode, inspection.InspectionTypeCode, out var definition))
            {
                continue;
            }

            Contexts[inspection.Id] = new InspectionCardContext
            {
                Discriminator = definition.Discriminator,
                PartialViewName = definition.PartialViewName,
                DataTypeName = definition.DataType.Name,
                ExhibitSequenceNumber = exhibit.SequenceNumber,
                EvidenceDescription = evidence.Description,
                EvidenceTypeDisplayName = evidenceTypeNames.GetOrDefault(evidence.EvidenceTypeCode)
                                          ?? evidence.EvidenceTypeCode,
                InspectionTypeDisplayName = inspectionTypeNames.GetOrDefault(inspection.InspectionTypeCode)
                                            ?? inspection.InspectionTypeCode
            };
        }
    }

    /// <summary>
    /// Everything Create.js needs to keep the dependent dropdowns and the data cards in step with
    /// the structure, without asking the server.
    /// </summary>
    private void BuildClientConfig()
    {
        var config = new
        {
            inspectionTypesByEvidenceType = InspectionTypeOptions.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Select(o => new { code = o.Value, displayName = o.Text }).ToList()),

            evidenceTypeNames = EvidenceTypeOptions.ToDictionary(o => o.Value, o => o.Text),

            combinations = _registry.Definitions.ToDictionary(
                d => $"{d.EvidenceTypeCode}|{d.InspectionTypeCode}",
                d => new { discriminator = d.Discriminator, dataTypeName = d.DataType.Name })
        };

        ClientConfigJson = JsonSerializer.Serialize(config);
    }

    public InspectionCardContext? GetContext(Guid inspectionId)
    {
        return Contexts.GetOrDefault(inspectionId);
    }

    public List<SelectListItem> GetInspectionTypeOptions(string? evidenceTypeCode)
    {
        if (string.IsNullOrWhiteSpace(evidenceTypeCode) ||
            !InspectionTypeOptions.TryGetValue(evidenceTypeCode, out var options))
        {
            return new List<SelectListItem>();
        }

        return options;
    }
}
