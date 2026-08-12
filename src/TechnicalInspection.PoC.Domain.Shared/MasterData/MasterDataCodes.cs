namespace TechnicalInspection.PoC.MasterData;

/// <summary>
/// Compile-time constants for the seeded master-data codes. The codes themselves live in the
/// database as rows; these constants exist so the registry can declare combinations without
/// magic strings.
/// </summary>
public static class EvidenceTypeCodes
{
    public const string Weapon = "WEAPON";
    public const string Substance = "SUBSTANCE";
    public const string Document = "DOCUMENT";
}

public static class InspectionTypeCodes
{
    public const string Ballistic = "BALLISTIC";
    public const string Fingerprint = "FINGERPRINT";
    public const string ChemicalAnalysis = "CHEMICAL_ANALYSIS";
    public const string Handwriting = "HANDWRITING";
}
