using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;

namespace LoggerAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LoggerGenericTypeAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "LoggerGenericTypeAnalyzer";
    private const string Category = "Logging";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.LoggerGenericTypeAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.LoggerGenericTypeAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.LoggerGenericTypeAnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
                   id: DiagnosticId,
                   title: Title,
                   messageFormat: MessageFormat,
                   category: Category,
                   defaultSeverity: DiagnosticSeverity.Error,
                   isEnabledByDefault: true,
                   description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.ConstructorDeclaration);
    }

    private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        var constructorDeclarationSyntaxNode = (ConstructorDeclarationSyntax)context.Node;
        foreach (var parameter in constructorDeclarationSyntaxNode.ParameterList.Parameters)
        {
            var genericParameterType = parameter.Type as GenericNameSyntax;
            if (genericParameterType is null)
            {
                continue;
            }

            if (genericParameterType.TypeArgumentList.Arguments.Count != 1)
            {
                continue;
            }

            var parameterSymbol = context.SemanticModel.GetDeclaredSymbol(parameter, context.CancellationToken);
            if (parameterSymbol.Type.Name != "ILogger")
            {
                continue;
            }

            var classSymbol = context.SemanticModel.GetDeclaredSymbol(constructorDeclarationSyntaxNode.Parent, context.CancellationToken);

            var genericArgument = genericParameterType.TypeArgumentList.Arguments.First() as IdentifierNameSyntax;
            var genericArgumentType = context.SemanticModel.GetTypeInfo(genericArgument, context.CancellationToken);
            if (genericArgumentType.Type.Name.Equals(classSymbol.Name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, genericArgument.GetLocation(), classSymbol.Name));
        }
    }
}
