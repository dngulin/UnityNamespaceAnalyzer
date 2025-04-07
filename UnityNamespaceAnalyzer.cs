using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UnityNamespaceAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UnityNamespaceAnalyzer : DiagnosticAnalyzer
    {
        private static DiagnosticDescriptor Rule(int id, string title, string msg, DiagnosticSeverity severity)
        {
            return new DiagnosticDescriptor(
                id: $"NS{id:D2}",
                title: title,
                messageFormat: msg,
                category: "Naming",
                defaultSeverity: severity,
                isEnabledByDefault: true
            );
        }

        private static readonly DiagnosticDescriptor NamespaceRule = Rule(
            1, "Incorrect namespace", "Namespace should be `{0}`", DiagnosticSeverity.Warning
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            NamespaceRule
        );

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeNamespace, SyntaxKind.NamespaceDeclaration);
        }

        private static void AnalyzeNamespace(SyntaxNodeAnalysisContext ctx)
        {
            if (ctx.Compilation.AssemblyName == null)
                return;

            var path = ctx.Node.SyntaxTree.FilePath;
            if (string.IsNullOrEmpty(path))
                return;

            var directory = Path.GetDirectoryName(path);
            if (!TryFindAsmdefPath(directory, out var asmdefPath))
                return;

            if (!TryGetRequiredNamespace(asmdefPath, directory, out var requiredNs))
                return;

            var decl = (NamespaceDeclarationSyntax)ctx.Node;
            var declaredNs = decl.Name.ToString();

            if (declaredNs != requiredNs)
                ctx.ReportDiagnostic(Diagnostic.Create(NamespaceRule, decl.Name.GetLocation(), requiredNs));
        }

        private static bool TryFindAsmdefPath(string dir, out string asmdefPath)
        {
            for (var i = 0; i < 16; i++)
            {
                var names = Directory.GetFiles(dir, "*.asmdef", SearchOption.TopDirectoryOnly);
                if (names.Length > 0)
                {
                    asmdefPath = names[0];
                    return true;
                }

                var parent = Directory.GetParent(dir);
                if (parent == null)
                    break; // There is no parent directory

                if (Directory.GetFiles(dir, "*.sln", SearchOption.TopDirectoryOnly).Length > 0)
                    break; // Reached unity project root

                dir = parent.FullName;
            }

            asmdefPath = null;
            return false;
        }

        private static bool TryGetRequiredNamespace(string asmDefPath, string fileDir, out string ns)
        {
            if (!TryGetRootNamespace(asmDefPath, out var rootNs))
            {
                ns = null;
                return false;
            }

            var asmDefDir = Path.GetDirectoryName(asmDefPath);
            if (asmDefDir == fileDir)
            {
                ns = rootNs;
                return true;
            }

            if (!fileDir.StartsWith(asmDefDir))
            {
                ns = null;
                return false;
            }

            var diff = fileDir.Substring(asmDefDir.Length + 1);
            var suffix = diff.Replace(Path.PathSeparator, '.');

            ns = $"{rootNs}.{suffix}";
            return true;
        }

        private static bool TryGetRootNamespace(string asmDefPath, out string ns)
        {
            const string prefix = @"    ""rootNamespace"": """;
            const string suffix = @""",";
            var trimLength = prefix.Length + suffix.Length;

            foreach (var line in File.ReadAllLines(asmDefPath))
            {
                if (line.Length >= trimLength && line.StartsWith(prefix) && line.EndsWith(suffix))
                {
                    ns = line.Length == trimLength ? "" : line.Substring(prefix.Length, line.Length - trimLength);
                    return true;
                }
            }

            ns = null;
            return false;
        }
    }
}