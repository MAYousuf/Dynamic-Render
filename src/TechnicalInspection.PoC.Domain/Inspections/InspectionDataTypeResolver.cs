using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace TechnicalInspection.PoC.Inspections;

public class InspectionDataTypeResolver : IInspectionDataTypeResolver, ISingletonDependency
{
    private readonly Dictionary<(string EvidenceTypeCode, string InspectionTypeCode), InspectionDataDefinition> _byCombination;
    private readonly Dictionary<string, InspectionDataDefinition> _byDiscriminator;
    private readonly IInspectionDataRegistry _registry;

    public InspectionDataTypeResolver(IInspectionDataRegistry registry)
    {
        _registry = registry;

        _byCombination = registry.Definitions.ToDictionary(
            d => (d.EvidenceTypeCode, d.InspectionTypeCode));

        _byDiscriminator = registry.Definitions.ToDictionary(
            d => d.Discriminator,
            StringComparer.Ordinal);
    }

    public InspectionDataDefinition Resolve(string evidenceTypeCode, string inspectionTypeCode)
    {
        if (!TryResolve(evidenceTypeCode, inspectionTypeCode, out var definition))
        {
            throw new BusinessException(PoCDomainErrorCodes.UnsupportedInspectionCombination)
                .WithData("EvidenceTypeCode", evidenceTypeCode)
                .WithData("InspectionTypeCode", inspectionTypeCode);
        }

        return definition;
    }

    public bool TryResolve(
        string evidenceTypeCode,
        string inspectionTypeCode,
        [NotNullWhen(true)] out InspectionDataDefinition? definition)
    {
        if (evidenceTypeCode.IsNullOrWhiteSpace() || inspectionTypeCode.IsNullOrWhiteSpace())
        {
            definition = null;
            return false;
        }

        return _byCombination.TryGetValue((evidenceTypeCode, inspectionTypeCode), out definition);
    }

    public InspectionDataDefinition ResolveByDiscriminator(string discriminator)
    {
        if (!TryResolveByDiscriminator(discriminator, out var definition))
        {
            throw new BusinessException(PoCDomainErrorCodes.UnknownInspectionDiscriminator)
                .WithData("Discriminator", discriminator);
        }

        return definition;
    }

    public bool TryResolveByDiscriminator(
        string discriminator,
        [NotNullWhen(true)] out InspectionDataDefinition? definition)
    {
        if (discriminator.IsNullOrWhiteSpace())
        {
            definition = null;
            return false;
        }

        return _byDiscriminator.TryGetValue(discriminator, out definition);
    }

    public IReadOnlyList<InspectionDataDefinition> GetForEvidenceType(string evidenceTypeCode)
    {
        return _registry.Definitions
            .Where(d => d.EvidenceTypeCode == evidenceTypeCode)
            .ToList();
    }
}
