using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TechnicalInspection.PoC.Requests;

public enum ChemicalResult
{
    NotDetermined = 0,
    Positive = 1,
    Negative = 2,
    Inconclusive = 3
}

/// <summary>
/// Carries the conditional-field example from section 13 of the design: when the result is
/// Positive, additional findings become mandatory. The rule lives on the model itself
/// (via <see cref="IValidatableObject"/>) rather than in a page, so it is enforced no matter
/// which page or handler binds the type.
/// </summary>
public class ChemicalAnalysisInspectionData : InspectionData, IValidatableObject
{
    [Required]
    [StringLength(128)]
    [Display(Name = "Substance name")]
    public string? SubstanceName { get; set; }

    [Required]
    [Range(0.01, 100000)]
    [Display(Name = "Sample weight (g)")]
    public decimal? SampleWeight { get; set; }

    [StringLength(256)]
    [Display(Name = "Chemical composition")]
    public string? ChemicalComposition { get; set; }

    [Display(Name = "Result")]
    public ChemicalResult Result { get; set; }

    [StringLength(2000)]
    [Display(Name = "Additional findings")]
    public string? AdditionalFindings { get; set; }

    [StringLength(256)]
    [Display(Name = "Required follow-up tests")]
    public string? RequiredTests { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Result != ChemicalResult.Positive)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(AdditionalFindings))
        {
            yield return new ValidationResult(
                "Additional findings are required when the result is Positive.",
                new[] { nameof(AdditionalFindings) });
        }

        if (string.IsNullOrWhiteSpace(RequiredTests))
        {
            yield return new ValidationResult(
                "Required follow-up tests must be listed when the result is Positive.",
                new[] { nameof(RequiredTests) });
        }
    }
}
