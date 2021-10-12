using HandlebarsDotNet;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace KaneBlake.CodeAnalysis
{
    /// <summary>
    /// Auto Interface Implementation
    /// </summary>
    [Generator]
    public class AutoInterfaceImplementGenerator : ISourceGenerator
    {
        private static readonly DiagnosticDescriptor DiagnosticWarning =
            new DiagnosticDescriptor(DiagnosticIds.WarningRuleId,
                title: "Diagnostic Warning",
                messageFormat: "AutoInterfaceImplementGenerator: {0}",
                category: "AutoInterfaceImplementGenerator",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor DiagnosticInfo =
            new DiagnosticDescriptor(DiagnosticIds.InfoRuleId,
                title: "Diagnostic Info",
                messageFormat: "AutoInterfaceImplementGenerator: {0}",
                category: "AutoInterfaceImplementGenerator",
                DiagnosticSeverity.Info,
                isEnabledByDefault: true);

        public void Execute(GeneratorExecutionContext context)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticWarning,
                    Location.None,
                    $"ISourceGenerator.Execute {Assembly.GetExecutingAssembly().Location}"));

            // the generator infrastructure will create a receiver and populate it
            // we can retrieve the populated instance via the context
            if (context.SyntaxContextReceiver is not MySyntaxReceiver syntaxReceiver)
            {
                return;
            }

            List<(string GeneratorName, HbsLoadType loadType, AdditionalText file)> templates = 
                GetLoadOptions(context).Where(r => "AutoInterfaceImplement".Equals(r.GeneratorName)).ToList();

            var startupTemplateFile = templates.FirstOrDefault(r => r.loadType.Equals(HbsLoadType.Startup)).file;
            var partialTemplateFiles = templates.Where(r => r.loadType.Equals(HbsLoadType.Partial)).Select(r => r.file);

            if (startupTemplateFile is null)
            {
                return;
            }

            foreach (var partialTemplateFile in partialTemplateFiles)
            {
                var partialTemplateName = Path.GetFileNameWithoutExtension(partialTemplateFile.Path);
                var partialTemplateText = partialTemplateFile.GetText()?.ToString() ?? string.Empty;
                Handlebars.RegisterTemplate(partialTemplateName, partialTemplateText);
            }
            Handlebars.RegisterHelper("ifCond", (output, options, context, arguments) =>
            {
                if (arguments.Length < 3)
                {
                    return;
                }
                var arg0 = arguments[0];
                var arg1 = arguments[1];
                var arg2 = arguments[2];

                switch (arg1)
                {
                    case "Equals":
                        if (arg0.Equals(arg2))
                            options.Template(output, context);
                        else
                            options.Inverse(output, context);
                        break;
                }
            });
            var startupTemplateText = startupTemplateFile.GetText()?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(startupTemplateText)) 
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticWarning,
                        Location.None, 
                        $"template file '{startupTemplateFile.Path}' is empty"));
                return;
            }
            var startupTemplate = Handlebars.Compile(startupTemplateText);



            var autoImplementationInterfaces = syntaxReceiver.AutoImplementationInterfaces;

            var classHbsContexts = new List<ClassHbsContext>();

            foreach (var namedTypeSymbol in autoImplementationInterfaces)
            {
                var classHbsContext = new ClassHbsContext
                {
                    NameSpaceQualifiedIdentifier = namedTypeSymbol.ContainingNamespace.ToDisplayString(),
                    Identifier = namedTypeSymbol.Name,
                    Properties = new List<PropertyHbsContext>()
                };

                var propertySymbols = namedTypeSymbol.AllInterfaces
                    .Concat(new INamedTypeSymbol[] { namedTypeSymbol })
                    .SelectMany(interfaceSymbol => interfaceSymbol.GetMembers()
                    .OfType<IPropertySymbol>());

                var propertyInfos = propertySymbols.Select(
                    propertySymbol =>
                    {
                        var accessibilities = new Accessibility[]
                        {
                                propertySymbol.DeclaredAccessibility,
                                propertySymbol.GetMethod?.DeclaredAccessibility ?? Accessibility.Public,
                                propertySymbol.SetMethod?.DeclaredAccessibility ?? Accessibility.Public
                        };

                        var isPublic = accessibilities.All(a => a.Equals(Accessibility.Public));

                        var accessorList = (propertySymbol.GetMethod, propertySymbol.SetMethod) switch
                        {
                            (IMethodSymbol get, IMethodSymbol set) => new HashSet<string>() { "get", "set" },
                            (IMethodSymbol get, null) => new HashSet<string>() { "get" },
                            (null, IMethodSymbol set) => new HashSet<string>() { "set" },
                            (_, _) => new HashSet<string>()
                        };

                        return new PropertyDeclaration
                        {
                            IsPublic = isPublic,
                            AccessorList = accessorList,
                            IsClassMember = false,
                            IsMerged = false,
                            Symbol = propertySymbol
                        };
                    }).ToList();

                var classMembers = propertyInfos
                    .Where(r => r.IsPublic)
                    .GroupBy(r => r.Symbol.Name)
                    .Select(g1 => g1
                        .GroupBy(r => r.Symbol.Type.ToDisplayString())
                        .Select(g2 => new { PropertyDeclaration = g2.First(), InterfaceCount = g2.Count() })
                        .OrderByDescending(r => r.InterfaceCount)
                        .FirstOrDefault().PropertyDeclaration)
                    .ToList();


                (from c in classMembers
                 join p in propertyInfos.Where(r => r.IsPublic) on
                    new { c.Symbol.Name, Type = c.Symbol.Type.ToDisplayString() }
                    equals
                    new { p.Symbol.Name, Type = p.Symbol.Type.ToDisplayString() } into g
                 select MergeAccessorList(c, g))
                 .ToList();

                propertyInfos = propertyInfos.Where(p => !p.IsMerged).ToList();

                foreach (var propertyInfo in propertyInfos)
                {
                    var propertyHbsContext = new PropertyHbsContext
                    {
                        Accessors = propertyInfo.AccessorList,
                        TypeIdentifier = propertyInfo.Symbol.Type.ToDisplayString()
                    };
                    if (propertyInfo.IsClassMember)
                    {
                        propertyHbsContext.AccessModifiers = "public ";
                        propertyHbsContext.Identifier = propertyInfo.Symbol.Name;
                    }
                    else
                    {
                        propertyHbsContext.AccessModifiers = string.Empty;
                        propertyHbsContext.Identifier = propertyInfo.Symbol.ToDisplayString();
                    }
                    classHbsContext.Properties.Add(propertyHbsContext);
                }

                classHbsContexts.Add(classHbsContext);

                var classSourceCode = startupTemplate(classHbsContext);

                context.AddSource($"{classHbsContext.NameSpaceQualifiedIdentifier}.AutoImplementation{classHbsContext.Identifier}.cs", SourceText.From(classSourceCode, Encoding.UTF8));
            }

        }


        public void Initialize(GeneratorInitializationContext context)
        {
            // Debugger.Launch();
            // Register a factory that can create our custom syntax receiver
            context.RegisterForSyntaxNotifications(() => new MySyntaxReceiver());

        }


        private IEnumerable<(string GeneratorName, HbsLoadType, AdditionalText)> GetLoadOptions(GeneratorExecutionContext context)
        {
            foreach (AdditionalText file in context.AdditionalFiles)
            {
                if (Path.GetExtension(file.Path).Equals(".hbs", StringComparison.OrdinalIgnoreCase))
                {
                    // are there any options for it?
                    context.AnalyzerConfigOptions.GetOptions(file).TryGetValue("build_metadata.AdditionalFiles.GeneratorName", out var generatorName);

                    context.AnalyzerConfigOptions.GetOptions(file).TryGetValue("build_metadata.AdditionalFiles.HbsLoadType", out var loadTypeString);
                    Enum.TryParse(loadTypeString, ignoreCase: true, out HbsLoadType loadType);

                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticWarning,
                            Location.None,
                            $"template file '{file.Path}' generatorName '{generatorName}' loadTypeString '{loadTypeString}'"));

                    yield return (generatorName?.Trim(), loadType, file);
                }
            }
        }


        private PropertyDeclaration MergeAccessorList(PropertyDeclaration property, IEnumerable<PropertyDeclaration> properties)
        {
            property.IsClassMember = true;

            property.AccessorList = new HashSet<string>(properties
                .SelectMany(r => r.AccessorList));

            properties.Where(p => !p.IsClassMember).ToList().ForEach(p => p.IsMerged = true);

            return property;
        }


        private enum HbsLoadType
        {
            Startup,
            Partial
        }

        private class ClassHbsContext
        {
            public string NameSpaceQualifiedIdentifier { get; set; }

            public string Identifier { get; set; }

            public List<PropertyHbsContext> Properties { get; set; }
        }

        private class PropertyHbsContext
        {
            public string AccessModifiers { get; set; }

            public string TypeIdentifier { get; set; }

            public string Identifier { get; set; }

            public HashSet<string> Accessors { get; set; }
        }

        private class PropertyDeclaration
        {
            public IPropertySymbol Symbol { get; set; }

            public HashSet<string> AccessorList { get; set; }

            public bool IsPublic { get; set; }

            public bool IsClassMember { get; set; }

            public bool IsMerged { get; set; }
        }

    }

    internal class MySyntaxReceiver : ISyntaxContextReceiver
    {
        private static string _attributeName = typeof(AutoImplementationAttribute).FullName;

        public List<INamedTypeSymbol> AutoImplementationInterfaces { get; private set; } = new List<INamedTypeSymbol>();


        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is InterfaceDeclarationSyntax interfaceDeclarationSyntax && interfaceDeclarationSyntax.AttributeLists.Count > 0)
            {
                var declaredSymbol = context.SemanticModel.GetDeclaredSymbol(interfaceDeclarationSyntax);
                if (declaredSymbol is INamedTypeSymbol namedTypeSymbol) 
                {
                    if (namedTypeSymbol.GetAttributes().Any(ad => ad.AttributeClass.ToDisplayString().Equals(_attributeName))) 
                    {
                        AutoImplementationInterfaces.Add(namedTypeSymbol);
                    }
                }
            }
        }
    }

    public static class DiagnosticIds
    {
        public const string InfoRuleId = "KBSRCGEN0001";

        public const string WarningRuleId = "KBSRCGEN0002";
    }
}
