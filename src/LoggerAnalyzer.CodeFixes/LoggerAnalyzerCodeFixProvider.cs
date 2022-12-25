using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LoggerAnalyzer;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LoggerGenericTypeAnalyzerCodeFixProvider)), Shared]
public class LoggerGenericTypeAnalyzerCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create(LoggerGenericTypeAnalyzer.DiagnosticId);

    public sealed override FixAllProvider GetFixAllProvider()
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
        => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        var loggerGenericArgument = root.FindToken(diagnosticSpan.Start).Parent;

        var loggerParameter = loggerGenericArgument.AncestorsAndSelf().OfType<ParameterSyntax>().FirstOrDefault();
        if (loggerParameter == null)
            return;

        var @class = loggerGenericArgument.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (@class == null)
            return;

        var constructor = loggerGenericArgument.AncestorsAndSelf().OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
        if (constructor == null)
            return;

        var loggerAssignmentExpression = constructor.DescendantNodes()
                .OfType<ExpressionStatementSyntax>()
                .FirstOrDefault(e => e.Expression is AssignmentExpressionSyntax aes
                    && aes.Left is IdentifierNameSyntax
                    && aes.Right is IdentifierNameSyntax ins
                    && ins.Identifier.Text == loggerParameter.Identifier.Text);

        SyntaxNode loggerField = null;
        if (loggerAssignmentExpression != null)
        {
            var loggerAssignmentExpressionLeft = (IdentifierNameSyntax)((AssignmentExpressionSyntax)loggerAssignmentExpression.Expression).Left;

            loggerField = @class.DescendantNodes()
              .Where(n => (n is FieldDeclarationSyntax fds
                        && fds.Declaration.Variables.Count > 0
                        && fds.Declaration.Variables.First().Identifier.Text == loggerAssignmentExpressionLeft.Identifier.Text)
                  || (n is PropertyDeclarationSyntax pds && pds.Identifier.Text == loggerAssignmentExpressionLeft.Identifier.Text))
              .FirstOrDefault();
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                 CodeFixResources.CodeFixTitle,
                 _ => ChangeLoggerArgument(context.Document, root, loggerGenericArgument, @class, loggerField),
                 nameof(CodeFixResources.CodeFixTitle)),
            diagnostic);
    }

    private Task<Document> ChangeLoggerArgument(Document document,
                                                    SyntaxNode root,
                                                    SyntaxNode loggerGenericArgument,
                                                    TypeDeclarationSyntax @class,
                                                    SyntaxNode loggerField)
    {
        var loggerGenericArgumentIdentifier = loggerGenericArgument.ChildTokens().FirstOrDefault();
        if (loggerGenericArgumentIdentifier == null)
            return Task.FromResult(document);

        var newLoggerGenericArgumentIdentifier = SyntaxFactory.Identifier(
           loggerGenericArgument.GetLeadingTrivia(),
           @class.Identifier.Text,
           loggerGenericArgument.GetTrailingTrivia());

        SyntaxNode newRoot = null;
        if (loggerField == null)
        {
            newRoot = root.ReplaceToken(loggerGenericArgumentIdentifier, newLoggerGenericArgumentIdentifier);
        }
        else
        {
            var loggerFieldGenericType = loggerField switch
            {
                FieldDeclarationSyntax field => (GenericNameSyntax)field.Declaration.Type,
                PropertyDeclarationSyntax property => (GenericNameSyntax)property.Type,
                _ => default

            };

            var loggerFieldGenericArgument = loggerFieldGenericType.TypeArgumentList.Arguments.FirstOrDefault();
            if (loggerFieldGenericArgument == null)
                return Task.FromResult(document);

            var loggerFieldGenericArgumentIdentifier = loggerFieldGenericArgument.ChildTokens().First();

            var newlLoggerFieldGenericArgumentIdentifier = SyntaxFactory.Identifier(
                loggerFieldGenericArgument.GetLeadingTrivia(),
                @class.Identifier.Text,
                loggerFieldGenericArgument.GetTrailingTrivia());

            newRoot = root.ReplaceTokens(new[] { loggerGenericArgumentIdentifier, loggerFieldGenericArgumentIdentifier }, (t, _) =>
               {
                   if (t == loggerGenericArgumentIdentifier)
                       return newLoggerGenericArgumentIdentifier;
                   if (t == loggerFieldGenericArgumentIdentifier)
                       return newlLoggerFieldGenericArgumentIdentifier;
                   return default;
               });
        }

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
