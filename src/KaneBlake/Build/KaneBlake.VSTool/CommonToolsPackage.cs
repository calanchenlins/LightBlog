using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Task = System.Threading.Tasks.Task;

namespace KaneBlake.VSTool
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(CommonToolsPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class CommonToolsPackage : AsyncPackage
    {
        /// <summary>
        /// CommonToolsCommandPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "a19d89b6-be52-428d-bb41-9f20a7f95503";

        private DTE2 _dte2;

        internal DTE2 Dte2 => _dte2;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommonToolsPackage"/> class.
        /// </summary>
        public CommonToolsPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            _dte2 = await GetServiceAsync(typeof(DTE)) as DTE2;
            Assumes.Present(_dte2);
            if (_dte2 == null)
            {
                return;
            }


            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            //await CommonToolsCommand.InitializeAsync(this);

            if (await GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService oleMenuCommandService)
            {
                var menuCommandId1 = new CommandID(GuidList.guidDbContextPackageCmdSet,
                    (int)PkgCmdIDList.cmdidVstFileEncoding);
                var menuItem1 = new OleMenuCommand(OnProjectContextMenuInvokeHandler, menuCommandId1);
                oleMenuCommandService.AddCommand(menuItem1);

                var menuCommandId2 = new CommandID(GuidList.guidDbContextPackageCmdSet,
                    (int)PkgCmdIDList.cmdidVstFileEncodingAll);
                var menuItem2 = new OleMenuCommand(OnProjectContextMenuInvokeHandler, menuCommandId2);
                oleMenuCommandService.AddCommand(menuItem2);
            }
        }

        private void OnProjectContextMenuInvokeHandler(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!(sender is MenuCommand menuCommand) || _dte2.SelectedItems.Count != 1)
            {
                return;
            }

            var selectedProject = _dte2.SelectedItems.Item(1).Project;
            var selectedProjectItem = _dte2.SelectedItems.Item(1).ProjectItem;
            if (selectedProject == null) 
            {
                selectedProject = selectedProjectItem.ContainingProject;
            }

            if (selectedProject == null)
            {
                return;
            }

            var selectedProjectName = selectedProject.Name;

            // Locate LanguageService Version
            // https://github.com/dotnet/roslyn/blob/master/docs/wiki/NuGet-packages.md

            var componentModel = (IComponentModel)GetService(typeof(SComponentModel));
            Assumes.Present(componentModel);
            var workspace = componentModel.GetService<VisualStudioWorkspace>();
            var projects = workspace.CurrentSolution.Projects;

            // Code Refactoring In Project
            if (menuCommand.CommandID.ID == PkgCmdIDList.cmdidVstFileEncoding)
            {
                projects = projects.Where(p => p.Name.Equals(selectedProjectName));
            }
            else if (menuCommand.CommandID.ID == PkgCmdIDList.cmdidVstFileEncodingAll)
            {

            }
            else 
            {
                return;
            }

            var affectedFilesCount = 0;
            var commands = new List<string>();

            var dialogWindow = new ToolWindows.ReplaceDialog();
            dialogWindow.ShowModal();

            if (!dialogWindow.DialogResult.GetValueOrDefault()) 
            {
                return;
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var refactoringOptions = dialogWindow.ViewModel;

            projects = projects.Where(p => p.SupportsCompilation);

            var logContext = string.Empty;
            try 
            {
                var allDocuments = projects.Select(project => project.Documents.ToList()).DimensionReduction().ToList();


                if (refactoringOptions.AdjustNamespaces) 
                {
                    SynchronizeNamespaces(allDocuments, workspace.CurrentSolution);
                }


                var encoding = Encoding.GetEncoding(refactoringOptions.FileEncoding);

                // 修复文件编码
                for (var documentIndex = 0; documentIndex < allDocuments.Count; documentIndex++)
                {
                    var document = allDocuments[documentIndex];
                    logContext = document.FilePath;
                    var fileExtension = Path.GetExtension(document.FilePath);
                    var fileExtensions = new string[] { ".cs", ".vb" };
                    if (!document.FilePath.EndsWith(".cshtml.g.cs") && fileExtensions.Contains(fileExtension) && File.Exists(document.FilePath))
                    {
                        var fileName = Path.GetFileName(document.FilePath);
                        var text = document.GetTextAsync().ConfigureAwait(false).GetAwaiter().GetResult().ToString();
                        var newFilePath = Path.ChangeExtension(document.FilePath, $"{fileExtension}cp");

                        File.Delete(document.FilePath);

                        if (File.Exists(document.FilePath))
                        {
                            OutputGeneralPane($"File Delete failed: {document.FilePath}");
                        }

                        using (var sw = new StreamWriter(newFilePath, false, encoding))
                        {
                            sw.AutoFlush = true;
                            sw.Write(text);
                            sw.Flush();
                        }
                        if (!File.Exists(newFilePath))
                        {
                            OutputGeneralPane($"File Generate failed: {newFilePath}");
                        }

                        commands.Add($@"rename ""{newFilePath}"" ""{fileName}"" ");

                    }
                }
            }
            catch (Exception ex)
            {
                OutputGeneralPane($"File Fixe failed with Exception: {logContext}");
                OutputGeneralPane(ex.ToString());
            }


            using (var process = new System.Diagnostics.Process())
            {
                process.StartInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = "",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    // when RedirectStandardOutput set true, it will block RedirectStandardInput
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                };

                //process.StartInfo = new ProcessStartInfo()
                //{
                //    FileName = "sh",
                //    Arguments = "-c \"systemctl --version && dotnet --info\"",
                //    UseShellExecute = false,
                //    CreateNoWindow = true,
                //    RedirectStandardInput = true,
                //    // when RedirectStandardOutput set true, it will block RedirectStandardInput
                //    RedirectStandardOutput = false,
                //    RedirectStandardError = false,
                //};

                process.Start();
                process.StandardInput.AutoFlush = true;

                foreach (var commandText in commands)
                {
                    process.StandardInput.WriteLine(commandText);
                    process.StandardInput.Flush();

                    affectedFilesCount++;
                }

                process.StandardInput.WriteLine("exit");

                if (!process.WaitForExit(10000))
                {
                    process.Kill();
                }
            }

            stopwatch.Stop();

            // Show a message box to prove we were here
            VsShellUtilities.ShowMessageBox(
                this,
                $"Process Sucess.\n {affectedFilesCount} Files affected! Time:{stopwatch.ElapsedMilliseconds} ms",
                "",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private void SynchronizeNamespaces(List<Microsoft.CodeAnalysis.Document> documents, Microsoft.CodeAnalysis.Solution solution) 
        {
            var solutionProjects = solution.Projects.Where(p => p.SupportsCompilation);

            var usingDirectiveChangelogs = new Dictionary<string, Dictionary<string, string>>();

            var projectReferences = new Dictionary<string, List<string>>();

            // 修复文件的命名空间
            for (var documentIndex = 0; documentIndex < documents.Count; documentIndex++)
            {
                var document = documents[documentIndex];
                var project = document.Project;
                var projectNamespace = string.IsNullOrEmpty(project.DefaultNamespace) ? string.Empty : project.DefaultNamespace;//project.Name.Replace(" ", "_")
                var projectDir = Path.GetDirectoryName(project.FilePath);

                if (document.TryGetSyntaxRoot(out var syntaxRoot) && syntaxRoot is CompilationUnitSyntax compilationUnitSyntax)
                {
                    var memberDeclarationSyntaxNodes = compilationUnitSyntax.Members.ToArray();
                    var documentFixed = false;
                    var documentDir = Path.GetDirectoryName(document.FilePath);
                    for (var i = 0; i < memberDeclarationSyntaxNodes.Length; i++)
                    {
                        if (!(memberDeclarationSyntaxNodes[i] is NamespaceDeclarationSyntax namespaceDeclarationSyntaxNode))//BaseNamespaceDeclarationSyntax
                        {
                            continue;
                        }

                        var nameSpace = namespaceDeclarationSyntaxNode.Name.ToString();

                        var fixedNameSpace = projectNamespace;

                        if (documentDir.StartsWith(projectDir) && documentDir.Length > projectDir.Length)
                        {
                            var relativePath = documentDir.Substring(projectDir.Length);
                            fixedNameSpace = projectNamespace + relativePath.Replace(Path.DirectorySeparatorChar, '.').Replace(Path.AltDirectorySeparatorChar, '.');
                        }

                        if (nameSpace.Equals(fixedNameSpace))
                        {
                            continue;
                        }

                        var identifierNameSyntaxNodes = fixedNameSpace.Split('.').Where(n => !string.IsNullOrEmpty(n))
                            .Select(n => SyntaxFactory.IdentifierName(n)).ToArray();

                        if (identifierNameSyntaxNodes.Length <= 0)
                        {
                            continue;
                        }

                        NameSyntax nameSyntaxNode = identifierNameSyntaxNodes[0];

                        for (var ii = 1; ii < identifierNameSyntaxNodes.Length; ii++)
                        {
                            nameSyntaxNode = SyntaxFactory.QualifiedName(nameSyntaxNode, identifierNameSyntaxNodes[ii]);
                        }

                        if (!projectReferences.TryGetValue(project.FilePath, out var affectedProjects))
                        {
                            affectedProjects = solutionProjects.Where(p => p.AllProjectReferences.Any(r => r.ProjectId.Equals(project.Id))).Select(p => p.FilePath).ToList();
                            affectedProjects.Add(project.FilePath);
                            projectReferences.Add(project.FilePath, affectedProjects);
                        }


                        foreach (var affectedProject in affectedProjects)
                        {
                            if (!usingDirectiveChangelogs.TryGetValue(affectedProject, out var transformedNamespaces))
                            {
                                transformedNamespaces = new Dictionary<string, string>();
                                usingDirectiveChangelogs.Add(affectedProject, transformedNamespaces);
                            }
                            transformedNamespaces[nameSpace] = nameSyntaxNode.ToString();
                        }

                        nameSyntaxNode = nameSyntaxNode.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

                        memberDeclarationSyntaxNodes[i] = namespaceDeclarationSyntaxNode.WithName(nameSyntaxNode);

                        documentFixed = true;
                    }

                    if (documentFixed)
                    {
                        documents[documentIndex] = document
                            .WithSyntaxRoot(
                                compilationUnitSyntax.WithMembers(
                                    new SyntaxList<MemberDeclarationSyntax>(memberDeclarationSyntaxNodes)));
                    }
                }
            }


            var solutionDocuments = solutionProjects.Select(project => project.Documents.ToList()).DimensionReduction().ToList();

            // 修复解决方案中文件的命名空间引用
            for (var documentIndex = 0; documentIndex < solutionDocuments.Count; documentIndex++)
            {
                var document = solutionDocuments[documentIndex];
                if (!usingDirectiveChangelogs.TryGetValue(document.Project.FilePath, out var transformedNamespaces))
                {
                    continue;
                }
                if (document.TryGetSyntaxRoot(out var syntaxRoot) && syntaxRoot is CompilationUnitSyntax compilationUnitSyntax)
                {
                    var usingDirectiveSyntaxNodes = compilationUnitSyntax.Usings.ToArray();
                    var documentFixed = false;
                    var documentDir = Path.GetDirectoryName(document.FilePath);
                    for (var i = 0; i < usingDirectiveSyntaxNodes.Length; i++)
                    {
                        var usingDirectiveSyntaxNode = usingDirectiveSyntaxNodes[i];
                        var key = usingDirectiveSyntaxNode.Name.ToString();
                        if (transformedNamespaces.ContainsKey(key))
                        {
                            usingDirectiveSyntaxNodes[i] = usingDirectiveSyntaxNode.WithName(SyntaxFactory.IdentifierName(transformedNamespaces[key]));
                            documentFixed = true;
                        }
                    }
                    if (documentFixed)
                    {
                        solutionDocuments[documentIndex] = document
                            .WithSyntaxRoot(
                                compilationUnitSyntax.WithUsings(
                                    new SyntaxList<UsingDirectiveSyntax>(usingDirectiveSyntaxNodes)));
                    }
                }
            }

        }


        private void OutputGeneralPane(string text) 
        {
            OutputString(VSConstants.OutputWindowPaneGuid.GeneralPane_guid, text);
        }

        private void OutputDebugPane(string text)
        {
            OutputString(VSConstants.OutputWindowPaneGuid.DebugPane_guid, text);
        }

        private void OutputBuildPane(string text)
        {
            OutputString(VSConstants.OutputWindowPaneGuid.BuildOutputPane_guid, text);
        }

        /// <summary>
        /// https://docs.microsoft.com/zh-cn/visualstudio/extensibility/extending-the-output-window?view=vs-2019
        /// </summary>
        /// <param name="paneGuid"></param>
        /// <param name="text"></param>
        private void OutputString(Guid paneGuid, string text)// WriteToGeneralOutputWindowPane  LogMessageOutputWindow
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            const int VISIBLE = 1;
            const int DO_NOT_CLEAR_WITH_SOLUTION = 0;

            IVsOutputWindow outputWindow;
            IVsOutputWindowPane outputWindowPane = null;
            int hr;

            // The General pane is not created by default. 
            // Querying for SVsGeneralOutputWindowPane service will cause the General output pane to be created if it hasn't yet been created
            if (paneGuid.Equals(VSConstants.OutputWindowPaneGuid.GeneralPane_guid))
            {
                outputWindowPane = (IVsOutputWindowPane)GetService(typeof(SVsGeneralOutputWindowPane));
                Assumes.Present(outputWindowPane);
            }
            else 
            {
                outputWindow = (IVsOutputWindow)GetService(typeof(SVsOutputWindow));
                Assumes.Present(outputWindow);

                // Get the pane
                hr = outputWindow.GetPane(ref paneGuid, out outputWindowPane);

                if (outputWindowPane == null) 
                {
                    // Create a new pane if not exists
                    hr = outputWindow.CreatePane(ref paneGuid,"New Pane\n", VISIBLE, DO_NOT_CLEAR_WITH_SOLUTION);
                    ErrorHandler.ThrowOnFailure(hr);
                    outputWindow.GetPane(ref paneGuid, out outputWindowPane);
                }

                
            }

            // Output the text
            if (outputWindowPane != null)
            {
                outputWindowPane.Activate();
                outputWindowPane.OutputString(text + Environment.NewLine);
            }
        }


        #endregion
    }

    internal static class GuidList
    {
        public const string guidDbContextPackageCmdSetString = "f6b75515-a717-4baf-abf1-ffaaa774b8a5";

        public static readonly Guid guidDbContextPackageCmdSet = new Guid(guidDbContextPackageCmdSetString);
    }

    internal static class PkgCmdIDList
    {
        public const uint cmdidVstFileEncoding = 0x0002;
        public const uint cmdidVstFileEncodingAll = 0x0003;
    }

    internal static class EnumerableExtensions 
    {
        public static IEnumerable<T> DimensionReduction<T>(this IEnumerable<IEnumerable<T>> sources)
        {
            foreach (var source in sources)
            {
                foreach (var el in source)
                {
                    yield return el;
                }
            }
        }
    }
}
