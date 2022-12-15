using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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

        context.RegisterCompilationStartAction(context =>
        {
            var loggerSymbol = context.Compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.ILogger`1");
            if (loggerSymbol == null)
                return;

            context.RegisterSymbolAction(context =>
            {
                var symbol = (IMethodSymbol)context.Symbol;
                if (symbol.MethodKind != MethodKind.Constructor)
                    return;

                foreach (var parameter in symbol.Parameters)
                {
                    if (parameter.Type is not INamedTypeSymbol parameterType)
                        continue;

                    // Only consider ILogger<T> parameters
                    if (!parameterType.OriginalDefinition.Equals(loggerSymbol, SymbolEqualityComparer.Default))
                        continue;

                    // Check <T> is the containing class
                    if (parameterType.TypeArguments[0].Equals(symbol.ContainingType, SymbolEqualityComparer.Default))
                        continue;

                    // Try to report the type argument. If not possible, report the parameter
                    var syntax = parameter.DeclaringSyntaxReferences[0].GetSyntax();
                    if (syntax is ParameterSyntax { Type: GenericNameSyntax { TypeArgumentList.Arguments: var arguments } } && arguments.Count == 1)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Rule, arguments[0].GetLocation(), symbol.ContainingType.Name));
                        continue;
                    }

                    context.ReportDiagnostic(Diagnostic.Create(Rule, syntax.GetLocation(), symbol.ContainingType.Name));
                }
            }, SymbolKind.Method);
        });
    }
}
