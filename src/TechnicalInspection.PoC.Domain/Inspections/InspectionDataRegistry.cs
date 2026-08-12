using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using TechnicalInspection.PoC.MasterData;
using TechnicalInspection.PoC.Requests;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace TechnicalInspection.PoC.Inspections;

/// <summary>
/// Declares every supported inspection kind exactly once.
/// <para>
/// Adding a new inspection kind is a three-line change: derive a new class from
/// <see cref="InspectionData"/>, add its <c>[JsonDerivedType]</c> entry on that base class, and
/// add one row to <see cref="Combinations"/> here. Master data, type resolution, JSON
/// persistence, and Step 3 rendering all follow automatically.
/// </para>
/// <para>
/// Discriminators are not repeated here: they are read back from the <c>[JsonDerivedType]</c>
/// attributes, so the value used for binding can never drift from the value written to the
/// database.
/// </para>
/// </summary>
public class InspectionDataRegistry : IInspectionDataRegistry, ISingletonDependency
{
    private static readonly EvidenceTypeDescriptor[] EvidenceTypeDescriptors =
    {
        new(EvidenceTypeCodes.Weapon, "Weapon", 1),
        new(EvidenceTypeCodes.Substance, "Substance", 2),
        new(EvidenceTypeCodes.Document, "Document", 3)
    };

    private static readonly InspectionTypeDescriptor[] InspectionTypeDescriptors =
    {
        new(InspectionTypeCodes.Ballistic, "Ballistic Examination", 1),
        new(InspectionTypeCodes.Fingerprint, "Fingerprint Analysis", 2),
        new(InspectionTypeCodes.ChemicalAnalysis, "Chemical Analysis", 3),
        new(InspectionTypeCodes.Handwriting, "Handwriting Comparison", 4)
    };

    /// <summary>
    /// (evidence type, inspection type) -> concrete model + strongly typed partial.
    /// </summary>
    private static readonly (string EvidenceTypeCode, string InspectionTypeCode, Type DataType, string PartialViewName)[] Combinations =
    {
        (EvidenceTypeCodes.Weapon, InspectionTypeCodes.Ballistic,
            typeof(BallisticInspectionData), "_BallisticInspection"),

        (EvidenceTypeCodes.Weapon, InspectionTypeCodes.Fingerprint,
            typeof(FingerprintInspectionData), "_FingerprintInspection"),

        (EvidenceTypeCodes.Substance, InspectionTypeCodes.ChemicalAnalysis,
            typeof(ChemicalAnalysisInspectionData), "_ChemicalAnalysisInspection"),

        (EvidenceTypeCodes.Document, InspectionTypeCodes.Handwriting,
            typeof(HandwritingInspectionData), "_HandwritingInspection")
    };

    public IReadOnlyList<InspectionDataDefinition> Definitions { get; }

    public IReadOnlyList<EvidenceTypeDescriptor> EvidenceTypes => EvidenceTypeDescriptors;

    public IReadOnlyList<InspectionTypeDescriptor> InspectionTypes => InspectionTypeDescriptors;

    public InspectionDataRegistry()
    {
        var discriminatorsByType = ReadDiscriminatorsFromJsonAttributes();

        Definitions = Combinations
            .Select(c => new InspectionDataDefinition(
                c.EvidenceTypeCode,
                c.InspectionTypeCode,
                ResolveDiscriminator(discriminatorsByType, c.DataType),
                c.DataType,
                c.PartialViewName))
            .ToList();

        GuardAgainstInconsistencies(discriminatorsByType);
    }

    /// <summary>
    /// The <c>[JsonDerivedType]</c> attributes on <see cref="InspectionData"/> are the source of
    /// truth for persisted discriminators; this reads them rather than restating them.
    /// </summary>
    private static Dictionary<Type, string> ReadDiscriminatorsFromJsonAttributes()
    {
        return typeof(InspectionData)
            .GetCustomAttributes<JsonDerivedTypeAttribute>(inherit: false)
            .ToDictionary(
                a => a.DerivedType,
                a => a.TypeDiscriminator as string
                     ?? throw new AbpException(
                         $"[JsonDerivedType] for '{a.DerivedType.Name}' must use a string discriminator."));
    }

    private static string ResolveDiscriminator(Dictionary<Type, string> discriminatorsByType, Type dataType)
    {
        if (!discriminatorsByType.TryGetValue(dataType, out var discriminator))
        {
            throw new AbpException(
                $"'{dataType.Name}' is registered as an inspection model but has no " +
                $"[JsonDerivedType] entry on {nameof(InspectionData)}. Add one so the type can be " +
                "persisted and read back.");
        }

        return discriminator;
    }

    /// <summary>
    /// Fails fast at startup rather than at the moment a user reaches Step 3 with data that
    /// cannot be stored or read back.
    /// </summary>
    private void GuardAgainstInconsistencies(Dictionary<Type, string> discriminatorsByType)
    {
        var evidenceCodes = EvidenceTypeDescriptors.Select(d => d.Code).ToHashSet();
        var inspectionCodes = InspectionTypeDescriptors.Select(d => d.Code).ToHashSet();

        foreach (var definition in Definitions)
        {
            if (!evidenceCodes.Contains(definition.EvidenceTypeCode))
            {
                throw new AbpException(
                    $"Combination references unknown evidence type '{definition.EvidenceTypeCode}'.");
            }

            if (!inspectionCodes.Contains(definition.InspectionTypeCode))
            {
                throw new AbpException(
                    $"Combination references unknown inspection type '{definition.InspectionTypeCode}'.");
            }

            if (!definition.DataType.IsAssignableTo(typeof(InspectionData)) ||
                definition.DataType.IsAbstract)
            {
                throw new AbpException(
                    $"'{definition.DataType.Name}' must be a concrete subclass of {nameof(InspectionData)}.");
            }
        }

        var duplicateCombination = Definitions
            .GroupBy(d => (d.EvidenceTypeCode, d.InspectionTypeCode))
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateCombination != null)
        {
            throw new AbpException(
                $"Combination {duplicateCombination.Key} is declared more than once.");
        }

        var duplicateDiscriminator = Definitions
            .GroupBy(d => d.Discriminator)
            .FirstOrDefault(g => g.Select(d => d.DataType).Distinct().Count() > 1);

        if (duplicateDiscriminator != null)
        {
            throw new AbpException(
                $"Discriminator '{duplicateDiscriminator.Key}' maps to more than one type.");
        }

        var unusedTypes = discriminatorsByType.Keys
            .Except(Definitions.Select(d => d.DataType))
            .ToList();

        if (unusedTypes.Any())
        {
            throw new AbpException(
                $"These inspection models are serializable but unreachable because no combination " +
                $"maps to them: {unusedTypes.Select(t => t.Name).JoinAsString(", ")}.");
        }
    }
}
