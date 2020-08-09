using Tpd.Analyzer.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Tpd.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class HttpMethodAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "RSS001";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        internal const string Category = "Syntax";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
        }

        public void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
        {
            var classInfor = context.SemanticModel.GetDeclaredSymbol(context.Node);
            // Check Controller
            if (!classInfor.Name.EndsWith("Controller") ||
                classInfor.DeclaredAccessibility != Accessibility.Public)
            {
                return;
            }

            var methods = context.Node.ChildNodes().OfType<MethodDeclarationSyntax>().ToList();

            foreach (var method in methods)
            {
                AnalyzeMethodDeclaration(method, context);
            }
        }

        private void AnalyzeMethodDeclaration(MethodDeclarationSyntax method, SyntaxNodeAnalysisContext context)
        {
            // Only check public method
            var methodInfo = context.SemanticModel.GetDeclaredSymbol(method);
            if (methodInfo.DeclaredAccessibility != Accessibility.Public) return;

            var attributes = method.AttributeLists;
            var hasHttpVerb = false;
            if (attributes.Any())
            {
                var allAttributes = attributes.SelectMany(sm => sm.Attributes)
                    .Select(s => s.Name.ToString())
                    .ToList();

                hasHttpVerb = allAttributes.Any(w => Constants.HttpVerbs.Contains(w.ToString()));
            }

            if (!hasHttpVerb)
            {
                var diagnostics = Diagnostic.Create(Rule, method.GetLocation(), methodInfo.Name);
                context.ReportDiagnostic(diagnostics);
            }
        }
    }

}
