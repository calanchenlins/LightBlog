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
                var menuCommandId3 = new CommandID(GuidList.guidDbContextPackageCmdSet,
                    (int)PkgCmdIDList.cmdidKaneBlakeBuild);
                var menuItem3 = new OleMenuCommand(OnProjectContextMenuInvokeHandler, menuCommandId3);
                oleMenuCommandService.AddCommand(menuItem3);
            }
        }

        private void OnProjectContextMenuInvokeHandler(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!(sender is MenuCommand menuCommand) || _dte2.SelectedItems.Count != 1)
            {
                return;
            }

            var project = _dte2.SelectedItems.Item(1).Project;
            var projectItem = _dte2.SelectedItems.Item(1).ProjectItem;
            if (project == null) 
            {
                project = projectItem.ContainingProject;
            }

            if (project == null)
            {
                return;
            }

            var projectName = project.Name;

            if (menuCommand.CommandID.ID == PkgCmdIDList.cmdidKaneBlakeBuild)
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                // Locate LanguageService Version
                // https://github.com/dotnet/roslyn/blob/master/docs/wiki/NuGet-packages.md

                var componentModel = (IComponentModel)GetService(typeof(SComponentModel));
                Assumes.Present(componentModel);
                var workspace = componentModel.GetService<VisualStudioWorkspace>();
                var vsProject = workspace.CurrentSolution.Projects
                    .FirstOrDefault(p => p.Name.Equals(projectName));

                var projectDir = Path.GetDirectoryName(vsProject.FilePath);
                var affectedFilesCount = 0;

                var commands = new List<string>();
                foreach (var document in vsProject.Documents)
                {
                    var fileExtension = Path.GetExtension(document.FilePath);
                    var fileExtensions = new string[] { ".cs", ".vb" };
                    if (!document.FilePath.EndsWith(".cshtml.g.cs") && fileExtensions.Contains(fileExtension))
                    {
                        var fileName = Path.GetFileName(document.FilePath);
                        var text = document.GetTextAsync().ConfigureAwait(false).GetAwaiter().GetResult().ToString();
                        var newFilePath = Path.ChangeExtension(document.FilePath, $"{fileExtension}cp");

                        File.Delete(document.FilePath);

                        if (File.Exists(document.FilePath))
                        {
                            OutputGeneralPane($"File Delete failed: {document.FilePath}");
                        }

                        using (var sw = new StreamWriter(newFilePath, false, Encoding.UTF8))
                        {
                            sw.AutoFlush = true;
                            sw.WriteLine(text);
                            sw.Flush();
                        }
                        if (!File.Exists(newFilePath))
                        {
                            OutputGeneralPane($"File Generate failed: {newFilePath}");
                        }

                        commands.Add($@"rename {newFilePath} {fileName}");
                    }

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
        public const uint cmdidKaneBlakeBuild = 0x0100;
    }
}
