namespace TechnicalInspection.PoC.Requests;

/// <summary>
/// The controlled business discriminators written into the persisted JSON.
/// These are deliberately NOT CLR type names: renaming or moving a C# class must never
/// invalidate data already stored in the database. The trailing version segment leaves
/// room for a future shape change of the same inspection kind.
/// </summary>
public static class InspectionDataDiscriminators
{
    public const string Ballistic = "ballistic.v1";
    public const string Fingerprint = "fingerprint.v1";
    public const string ChemicalAnalysis = "chemical-analysis.v1";
    public const string Handwriting = "handwriting.v1";
}
