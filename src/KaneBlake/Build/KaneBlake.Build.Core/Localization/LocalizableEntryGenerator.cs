using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;


namespace KaneBlake.Build.Core.Localization
{
    public class LocalizableEntryGenerator : IVisitor
    {
        public int Order { get; set; } = 1000;

        public async Task Visit(DataStructure dataStructure)
        {
            var project = dataStructure.Project;
            var projectPath = dataStructure.ProjectDirectory;
            var POEntries = dataStructure.LocalizerEntries;
            var solution = project.Solution;
            var documents = project.Documents.Where(d => d.SupportsSyntaxTree);

            var LocalizerMethods = new List<LocalizationMethod>() {
                new LocalizationMethod("IStringLocalizer","get_Item",false),
                new LocalizationMethod("StringLocalizerExtensions","GetString",false),
                new LocalizationMethod("IHtmlLocalizer","get_Item",false),
                new LocalizationMethod("IHtmlLocalizer","GetString",false),
                new LocalizationMethod("ValidationAttribute","set_ErrorMessage",false),
                new LocalizationMethod("DisplayAttribute","set_Name",false),
                new LocalizationMethod("DisplayAttribute","set_Prompt",false)
            };

            var combinedMethodSymbols = Array.Empty<IMethodSymbol>().AsEnumerable();
            foreach (var localizerMethod in LocalizerMethods)
            {
                // find DeclarationSymbols
                var TypeDeclarationSymbols = await SymbolFinder.FindDeclarationsAsync(project, localizerMethod.TypeName, ignoreCase: false);
                var TypeDeclarationSymbol = TypeDeclarationSymbols.OfType<INamedTypeSymbol>().FirstOrDefault(s => s.IsGenericType.Equals(localizerMethod.IsGenericType));

                // find MethodSymbols
                var methodSymbols = TypeDeclarationSymbol?.GetMembers(localizerMethod.MethodName).OfType<IMethodSymbol>();

                // combine MethodSymbols 
                combinedMethodSymbols = combinedMethodSymbols.Union(methodSymbols);
            }

            foreach (var methodSymbol in combinedMethodSymbols)
            {
                //find method referenced location
                var methodReferencedLocations = (await SymbolFinder.FindReferencesAsync(methodSymbol, solution)).Where(r => r.Locations.Any());

                foreach (var referencedSymbol in methodReferencedLocations)
                {
                    foreach (var location in referencedSymbol.Locations)
                    {
                        var textSpan = location.Location.SourceSpan;
                        var document = location.Document;
                        var rootSyntaxNode = await document.GetSyntaxRootAsync();
                        var syntaxTree = await document.GetSyntaxTreeAsync();

                        var node = rootSyntaxNode.FindNode(textSpan);
                        var parent = node.Parent;
                        var lineNumber = node.GetLocation().GetLineSpan().StartLinePosition.Line;

                        var sourceFile = Path.IsPathRooted(syntaxTree.FilePath) ? Path.GetRelativePath(projectPath, syntaxTree.FilePath) : syntaxTree.FilePath;
                        var sourceFileLine = lineNumber + 1;
                        var code = syntaxTree.GetText().Lines[lineNumber].ToString().Trim();

                        // mapping *.cshtml.g.cs to *.cshtml
                        if (sourceFile.EndsWith(".cshtml.g.cs") && rootSyntaxNode.GetLeadingTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.PragmaChecksumDirectiveTrivia)).GetStructure() is PragmaChecksumDirectiveTriviaSyntax pragmaChecksumDirectiveTriviaSyntax)
                        {
                            var razorSourceFile = pragmaChecksumDirectiveTriviaSyntax.File.ValueText;

                            if (string.IsNullOrEmpty(razorSourceFile) && rootSyntaxNode is CompilationUnitSyntax compilationUnitSyntax)
                            {
                                var attributeList = compilationUnitSyntax.AttributeLists
                                    .Select(list => list.Attributes.FirstOrDefault(a => a.ToString().Contains("Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute")))
                                    .FirstOrDefault(a => a != null && a.ArgumentList.Arguments.Count == 3);
                                if (attributeList?.ArgumentList.Arguments[2].Expression is LiteralExpressionSyntax literalExpressionSyntax)
                                {
                                    razorSourceFile = Path.Combine(projectPath, literalExpressionSyntax.Token.ValueText[1..^0].Replace('/', Path.DirectorySeparatorChar));
                                }
                            }

                            if (File.Exists(razorSourceFile))
                            {
                                lineNumber = node.GetLocation().GetMappedLineSpan().StartLinePosition.Line;
                                sourceFileLine = lineNumber + 1;
                                sourceFile = Path.GetRelativePath(projectPath, razorSourceFile);
                                using var sr = new StreamReader(razorSourceFile, Encoding.UTF8);
                                while (lineNumber-- >= 0)
                                {
                                    code = await sr.ReadLineAsync();
                                }
                            }
                        }

                        var entry = new LocalizableEntry()
                        {
                            SourceReference = sourceFile + ":" + sourceFileLine,
                            SourceCode = code.Trim(),
                            ContextId = string.Empty,
                            Id = string.Empty
                        };

                        // find id and ContextId

                        switch (parent)
                        {
                            // IStringLocalizer.GetString("id")
                            case MemberAccessExpressionSyntax memberAccessExpressionSyntax
                                when memberAccessExpressionSyntax.Parent is InvocationExpressionSyntax invocationExpressionSyntax
                                && invocationExpressionSyntax.ArgumentList.Arguments.FirstOrDefault()?.Expression is LiteralExpressionSyntax literalExpressionSyntax:
                                entry.Id = literalExpressionSyntax.Token.ValueText;
                                entry.ContextId = (await FindLocalizerContextIdAsync(document, memberAccessExpressionSyntax.Expression.SpanStart, sourceFile));
                                break;

                            // IStringLocalizer["id"]
                            case ElementAccessExpressionSyntax elementAccessExpressionSyntax
                                when elementAccessExpressionSyntax.ArgumentList.Arguments.FirstOrDefault()?.Expression is LiteralExpressionSyntax literalExpressionSyntax2:
                                entry.Id = literalExpressionSyntax2.Token.ValueText;
                                entry.ContextId = (await FindLocalizerContextIdAsync(document, elementAccessExpressionSyntax.Expression.SpanStart, sourceFile));
                                break;

                            // ErrorMessage = "error": DataAnnotations without ContextId
                            case NameEqualsSyntax nameEqualsSyntax:
                                entry.Id = ((nameEqualsSyntax.Parent as AttributeArgumentSyntax).Expression as LiteralExpressionSyntax).Token.ValueText;
                                break;
                            default:
                                Console.WriteLine("find not supported SyntaxNode:" + parent.Parent.ToFullString());
                                continue;
                        }

                        POEntries.Add(entry);
                    }
                }
            }
        }

        private async Task<string> FindLocalizerContextIdAsync(Document document, int position, string sourceFile = null)
        {
            static string BuildBaseName(string path)
            {
                var extension = Path.GetExtension(path);
                var startIndex = path[0] == '/' || path[0] == '\\' ? 1 : 0;
                var length = path.Length - startIndex - extension.Length;
                var capacity = length;
                var builder = new StringBuilder(path, startIndex, length, capacity);

                builder.Replace('/', '.').Replace('\\', '.');

                return builder.ToString();
            }

            sourceFile ??= document.FilePath;

            var defineSymbol = await SymbolFinder.FindSymbolAtPositionAsync(document, position);
            var defineTypeSymbol = defineSymbol switch
            {
                ILocalSymbol localSymbol => localSymbol.Type,
                IPropertySymbol propertySymbol => propertySymbol.Type,
                IFieldSymbol fieldSymbol => fieldSymbol.Type,
                _ => null
            };

            if (defineTypeSymbol is INamedTypeSymbol namedTypeSymbol)
            {
                if (namedTypeSymbol.IsGenericType)
                {
                    return namedTypeSymbol.TypeArguments.FirstOrDefault()?.ToDisplayString() ?? string.Empty;
                }

                if (namedTypeSymbol.Name.Equals("IViewLocalizer"))
                {
                    return BuildBaseName(sourceFile);
                }
            }
            else
            {
                Console.WriteLine($"find not supported defineSymbol: {defineSymbol.Name} {defineSymbol.Kind}");
            }

            return string.Empty;
        }

    }
}
