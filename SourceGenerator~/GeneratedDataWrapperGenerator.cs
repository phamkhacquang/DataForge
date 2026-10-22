using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataForge.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class GeneratedDataWrapperGenerator : IIncrementalGenerator {
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var assemblyAndTypes = context.CompilationProvider.Select((compilation, _) => {
            if (!string.Equals(compilation.AssemblyName, "Assembly-CSharp", StringComparison.Ordinal)) {
                return Array.Empty<INamedTypeSymbol>();
            }

            var baseDataType = compilation.GetTypeByMetadataName("DataForge.BaseData`2");
            if (baseDataType == null) {
                return [];
            }

            var types = new List<INamedTypeSymbol>();
            CollectTypes(compilation.Assembly.GlobalNamespace, baseDataType, types);
            return [.. types
                .GroupBy(type => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), StringComparer.Ordinal)
                .Select(group => group.First())
                .OrderBy(type => type.ToDisplayString(), StringComparer.Ordinal)];
        });

        context.RegisterSourceOutput(assemblyAndTypes, (spc, dataTypes) => {
            spc.AddSource("GeneratedDataWrapper.g.cs", SourceText.From(RenderSource(dataTypes), Encoding.UTF8));
        });
    }

    private static void CollectTypes(INamespaceSymbol namespaceSymbol, INamedTypeSymbol baseDataType, List<INamedTypeSymbol> result) {
        foreach (var typeMember in namespaceSymbol.GetTypeMembers()) {
            CollectTypes(typeMember, baseDataType, result);
        }

        foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers()) {
            CollectTypes(nestedNamespace, baseDataType, result);
        }
    }

    private static void CollectTypes(INamedTypeSymbol typeSymbol, INamedTypeSymbol baseDataType, List<INamedTypeSymbol> result) {
        if (typeSymbol.TypeKind == TypeKind.Class
            && !typeSymbol.IsAbstract
            && HasSerializableAttribute(typeSymbol)
            && InheritsFrom(typeSymbol, baseDataType)) {
            result.Add(typeSymbol);
        }

        foreach (var nestedType in typeSymbol.GetTypeMembers()) {
            CollectTypes(nestedType, baseDataType, result);
        }
    }

    private static bool HasSerializableAttribute(INamedTypeSymbol typeSymbol) {
        foreach (var attribute in typeSymbol.GetAttributes()) {
            if (attribute.AttributeClass?.ToDisplayString() == typeof(SerializableAttribute).FullName) {
                return true;
            }
        }
        return false;
    }

    private static bool InheritsFrom(INamedTypeSymbol typeSymbol, INamedTypeSymbol baseDataType) {
        var baseType = typeSymbol.BaseType;
        return baseType != null
            && baseType.OriginalDefinition.Equals(baseDataType, SymbolEqualityComparer.Default);
    }

    private static string RenderSource(IReadOnlyList<INamedTypeSymbol> dataTypes) {
        var builder = new StringBuilder();
        builder.AppendLine("#if UNITY_EDITOR");
        builder.AppendLine("[System.Serializable]");
        builder.AppendLine("public sealed class GeneratedDataWrapper : UnityEngine.ScriptableObject {");

        var fieldTemplate = "    public {0} f{1};";
        for (int i = 0; i < dataTypes.Count; i++) {
            var globalName = dataTypes[i].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            builder.AppendLine(string.Format(fieldTemplate, globalName, i));
        }

        builder.AppendLine("}");
        builder.AppendLine("#endif");
        return builder.ToString();
    }
}