namespace TechnicalInspection.PoC;

public static class PoCDomainErrorCodes
{
    /* You can add your business exception error codes here, as constants */

    public const string UnsupportedInspectionCombination = "PoC:Inspections:UnsupportedCombination";
    public const string UnknownInspectionDiscriminator = "PoC:Inspections:UnknownDiscriminator";
    public const string InspectionDataTypeMismatch = "PoC:Inspections:DataTypeMismatch";
    public const string RequestHasNoExhibits = "PoC:Requests:NoExhibits";
    public const string RequestIncomplete = "PoC:Requests:Incomplete";
    public const string InspectionDataMissing = "PoC:Requests:InspectionDataMissing";
}
