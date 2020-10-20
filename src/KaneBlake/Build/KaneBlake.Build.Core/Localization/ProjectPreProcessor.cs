using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Razor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KaneBlake.Build.Core.Localization
{
    public class ProjectPreProcessor : IVisitor
    {
        public int Order { get; set; } = int.MinValue;

        public async Task Visit(DataStructure dataStructure)
        {
            dataStructure.Project = await OpenProjectWithRazorGenerateCommandAsync(dataStructure.ProjectDirectory);
        }

        private async Task<Project> OpenProjectWithRazorGenerateCommandAsync(string projectDirectory)
        {
            var startInfo = new ProcessStartInfo()
            {
                FileName = "dotnet",
                Arguments = "msbuild /target:restore;RazorGenerate /property:Configuration=Release",
                UseShellExecute = false,
                WorkingDirectory = projectDirectory,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var process = Process.Start(startInfo);
            process.WaitForExit();

            Project project = null;
            var projectFilePath = Directory.GetFiles(projectDirectory, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (projectFilePath == null)
            {
                return project;
            }

            var razorCSFiles = Directory.GetFiles(Path.Combine(projectDirectory, "obj", "Release"), "*.cshtml.g.cs", SearchOption.AllDirectories);
            var razorCSCodes = new string[razorCSFiles.Length];
            for (int i = 0; i < razorCSFiles.Length; i++)
            {
                using var sr = new StreamReader(razorCSFiles[i], Encoding.UTF8);
                razorCSCodes[i] = await sr.ReadToEndAsync();
            }

            //AnalysisSemanticModel
            using var msbws = MSBuildWorkspace.Create();
            msbws.SkipUnrecognizedProjects = true;
            msbws.LoadMetadataForReferencedProjects = true;
            msbws.WorkspaceFailed += (object sender, WorkspaceDiagnosticEventArgs e) => Console.WriteLine("ERR" + ":" + e.Diagnostic.Message);

            // load project file
            project = await msbws.OpenProjectAsync(projectFilePath);

            Console.WriteLine("-------------------------------------------");
            var diagnostics = msbws.Diagnostics;
            foreach (var diagnostic in diagnostics)
            {
                Console.WriteLine(diagnostic.Message);
            }

            foreach (var code in razorCSCodes)
            {
                project = project.AddDocument(Path.ChangeExtension(Path.GetRandomFileName(), ".cshtml.g.cs"), code).Project;
            }
            return project;
        }

        private async Task<Project> OpenProjectWithRazorFileAsync(string projectDirectory)
        {
            Project project = null;
            var projectFilePath = Directory.GetFiles(projectDirectory, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (projectFilePath == null)
            {
                return project;
            }

            var razorFiles = Directory.GetFiles(Path.Combine(projectDirectory), "*.cshtml", SearchOption.AllDirectories);
            var razorCSCodes = new string[razorFiles.Length];

            IEnumerable<MetadataReference> references = Array.Empty<MetadataReference>();
            using (var msbws = MSBuildWorkspace.Create())
            {
                msbws.SkipUnrecognizedProjects = true;
                msbws.LoadMetadataForReferencedProjects = true;
                msbws.WorkspaceFailed += (object sender, WorkspaceDiagnosticEventArgs e) => Console.WriteLine("ERR" + ":" + e.Diagnostic.Message);

                // load project file
                project = await msbws.OpenProjectAsync(projectDirectory);
                var compilation = await project.GetCompilationAsync();

                Console.WriteLine("-------------------------------------------");
                var diagnostics = msbws.Diagnostics;
                foreach (var diagnostic in diagnostics)
                {
                    Console.WriteLine(diagnostic.Message);
                }

                references = compilation.References;
            }

            // discover tagHelpers
            var fileSystem = RazorProjectFileSystem.Create(projectDirectory);
            var engine = RazorProjectEngine.Create(RazorConfiguration.Default, fileSystem, b =>
            {
                b.Features.Add(new DefaultMetadataReferenceFeature() { References = references.ToList() });
                b.Features.Add(new CompilationTagHelperFeature());
                b.Features.Add(new DefaultTagHelperDescriptorProvider());

                CompilerFeatures.Register(b);
            });
            var feature = engine.Engine.Features.OfType<ITagHelperFeature>().Single();
            var tagHelpers = feature.GetDescriptors();

            // generate C# code
            var razorConfiguration = RazorConfiguration.Create(
                RazorLanguageVersion.Latest,
                "MVC-3.0",
                new[] { new AssemblyExtension("MVC-3.0", typeof(RazorExtensions).Assembly) });

            engine = RazorProjectEngine.Create(razorConfiguration, fileSystem, b =>
            {
                b.Features.Add(new StaticTagHelperFeature() { TagHelpers = tagHelpers, });
                //CompilerFeatures.Register(b);
            });

            foreach (var file in razorFiles)
            {
                var item = engine.FileSystem.GetItem(file, "cshtml");
                var razorCode = engine.Process(item).GetCSharpDocument().GeneratedCode;

                project = project.AddDocument(Path.ChangeExtension(Path.GetRandomFileName(), ".cshtml.g.cs"), razorCode).Project;
            }
            return project;
        }

        private class StaticTagHelperFeature : ITagHelperFeature
        {
            public RazorEngine Engine { get; set; }

            public IReadOnlyList<TagHelperDescriptor> TagHelpers { get; set; }

            public IReadOnlyList<TagHelperDescriptor> GetDescriptors() => TagHelpers;
        }
    }
}
