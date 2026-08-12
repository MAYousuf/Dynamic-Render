using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using TechnicalInspection.PoC.MasterData;
using Volo.Abp;
using Volo.Abp.Modularity;
using Xunit;

namespace TechnicalInspection.PoC.Requests;

/// <summary>
/// Covers the single write in the flow: a whole request goes in, one row per inspection comes back
/// out of the JSON column as the concrete model its combination resolves to.
/// </summary>
public abstract class RequestAppServiceTests<TStartupModule> : PoCApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IRequestAppService _requestAppService;

    protected RequestAppServiceTests()
    {
        _requestAppService = GetRequiredService<IRequestAppService>();
    }

    [Fact]
    public async Task Should_Persist_Every_Inspection_As_Its_Own_Concrete_Model()
    {
        var requestId = await _requestAppService.SubmitAsync(BuildRequest());

        var detail = await _requestAppService.GetDetailAsync(requestId);

        detail.ShouldNotBeNull();
        detail.RequestNumber.ShouldBe("REQ-TEST-001");
        detail.Status.ShouldBe(InspectionRequestStatus.Submitted);
        detail.Exhibits.Count.ShouldBe(1);

        var inspections = detail.Exhibits
            .SelectMany(e => e.Evidences)
            .SelectMany(v => v.Inspections)
            .ToList();

        inspections.Count.ShouldBe(3);
        inspections.ShouldAllBe(i => i.DeserializationError == null);

        var ballistic = inspections.Single(i => i.InspectionTypeCode == InspectionTypeCodes.Ballistic);
        ballistic.DataDiscriminator.ShouldBe(InspectionDataDiscriminators.Ballistic);
        var ballisticData = ballistic.Data.ShouldBeOfType<BallisticInspectionData>();
        ballisticData.Caliber.ShouldBe("9mm");
        ballisticData.RoundsRecovered.ShouldBe(4);

        var fingerprint = inspections.Single(i => i.InspectionTypeCode == InspectionTypeCodes.Fingerprint);
        var fingerprintData = fingerprint.Data.ShouldBeOfType<FingerprintInspectionData>();
        fingerprintData.FingerprintClassification.ShouldBe("Loop");
        fingerprintData.NumberOfPrints.ShouldBe(2);

        var handwriting = inspections.Single(i => i.InspectionTypeCode == InspectionTypeCodes.Handwriting);
        var handwritingData = handwriting.Data.ShouldBeOfType<HandwritingInspectionData>();
        handwritingData.ReferenceSample.ShouldBe("Sample B");

        // Same column, three different shapes.
        inspections.Select(i => i.DataTypeName).Distinct().Count().ShouldBe(3);
    }

    [Fact]
    public async Task Should_Appear_In_The_Submitted_List()
    {
        await _requestAppService.SubmitAsync(BuildRequest());

        var list = await _requestAppService.GetSubmittedListAsync();

        var request = list.ShouldHaveSingleItem();
        request.RequestNumber.ShouldBe("REQ-TEST-001");
        request.InspectionCount.ShouldBe(3);
    }

    [Fact]
    public async Task Should_Reject_Data_That_Does_Not_Match_Its_Combination()
    {
        var input = BuildRequest();

        // A weapon/ballistic inspection carrying a handwriting model: nothing would be able to read
        // this row back, so it must not be written in the first place.
        input.Exhibits[0].Evidences[0].Inspections[0].Data = new HandwritingInspectionData
        {
            DocumentDescription = "Wrong model",
            ReferenceSample = "Wrong model"
        };

        var exception = await Should.ThrowAsync<BusinessException>(
            () => _requestAppService.SubmitAsync(input));

        exception.Code.ShouldBe(PoCDomainErrorCodes.InspectionDataTypeMismatch);

        (await _requestAppService.GetSubmittedListAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Reject_An_Unsupported_Combination()
    {
        var input = BuildRequest();

        // Chemical analysis is not offered for weapons.
        input.Exhibits[0].Evidences[0].Inspections[0].InspectionTypeCode =
            InspectionTypeCodes.ChemicalAnalysis;

        var exception = await Should.ThrowAsync<BusinessException>(
            () => _requestAppService.SubmitAsync(input));

        exception.Code.ShouldBe(PoCDomainErrorCodes.UnsupportedInspectionCombination);
    }

    [Fact]
    public async Task Should_Reject_A_Request_Without_Exhibits()
    {
        var input = BuildRequest();
        input.Exhibits.Clear();

        var exception = await Should.ThrowAsync<BusinessException>(
            () => _requestAppService.SubmitAsync(input));

        exception.Code.ShouldBe(PoCDomainErrorCodes.RequestHasNoExhibits);
    }

    private static CreateInspectionRequestDto BuildRequest()
    {
        return new CreateInspectionRequestDto
        {
            RequestNumber = "REQ-TEST-001",
            Subject = "Case 2026/114 - armed robbery",
            RequestDate = new System.DateTime(2026, 8, 12),
            Exhibits =
            {
                new CreateExhibitDto
                {
                    SequenceNumber = 1,
                    Description = "Items seized at the scene",
                    Evidences =
                    {
                        new CreateEvidenceDto
                        {
                            EvidenceTypeCode = EvidenceTypeCodes.Weapon,
                            Description = "Pistol found in the vehicle",
                            Inspections =
                            {
                                new CreateInspectionDto
                                {
                                    InspectionTypeCode = InspectionTypeCodes.Ballistic,
                                    Data = new BallisticInspectionData
                                    {
                                        WeaponType = "Pistol",
                                        Caliber = "9mm",
                                        SerialNumber = "ABC123",
                                        RoundsRecovered = 4,
                                        Findings = "Striations match the recovered casings."
                                    }
                                },
                                new CreateInspectionDto
                                {
                                    InspectionTypeCode = InspectionTypeCodes.Fingerprint,
                                    Data = new FingerprintInspectionData
                                    {
                                        FingerprintClassification = "Loop",
                                        NumberOfPrints = 2,
                                        LiftedSuccessfully = true,
                                        Findings = "Two usable prints on the grip."
                                    }
                                }
                            }
                        },
                        new CreateEvidenceDto
                        {
                            EvidenceTypeCode = EvidenceTypeCodes.Document,
                            Description = "Handwritten note",
                            Inspections =
                            {
                                new CreateInspectionDto
                                {
                                    InspectionTypeCode = InspectionTypeCodes.Handwriting,
                                    Data = new HandwritingInspectionData
                                    {
                                        DocumentDescription = "Note left at the counter",
                                        ReferenceSample = "Sample B",
                                        MatchConfidence = 82,
                                        Conclusion = "Probable match."
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
    }
}
