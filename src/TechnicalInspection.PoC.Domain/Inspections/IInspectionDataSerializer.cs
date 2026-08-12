using System;
using TechnicalInspection.PoC.Requests;

namespace TechnicalInspection.PoC.Inspections;

public interface IInspectionDataSerializer
{
    /// <summary>
    /// Serializes through the base type so the business discriminator is embedded in the JSON.
    /// </summary>
    string Serialize(InspectionData data);

    /// <summary>
    /// Discriminator-driven read: needs no context beyond the JSON itself.
    /// </summary>
    InspectionData Deserialize(string json);

    /// <summary>
    /// Resolver-driven read (section 11): the caller has already resolved the expected type from
    /// (EvidenceType, InspectionType), and this verifies the stored data really is that shape.
    /// </summary>
    InspectionData DeserializeAs(string json, Type expectedType);

    /// <summary>
    /// Reads only the embedded discriminator, without materializing the object.
    /// </summary>
    string? ReadDiscriminator(string json);

    /// <summary>Re-serializes with indentation, for display on the review screen.</summary>
    string Prettify(string json);
}
