using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT_PrebuildScripts
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class RunPostScritps
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4150;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("d70b81c5-3bdb-408f-a88d-d1c5c21bd366");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="RunPostScritps"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private RunPostScritps(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static RunPostScritps Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in RunPostScritps's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new RunPostScritps(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            EnvDTE.DTE dte = Package.GetGlobalService(typeof(DTE)) as DTE;
            Solution solution = dte.Solution;
            SolutionBuild solutionBuild = solution.SolutionBuild;
            string solutionFullPath = solution.FullName;
            string solutionLocation = solutionFullPath.Substring(0, solutionFullPath.LastIndexOf('\\'));

            //get the post scripts from file
            List<string> postScripts = new List<string>();
            string postScriptsPath = solutionLocation + "\\post_build_scripts.txt";
            if (File.Exists(postScriptsPath))
            {
                using (StreamReader reader = new StreamReader(postScriptsPath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        postScripts.Add(line);
                    }
                }
            }

            //execute the post scripts
            foreach (string script in postScripts)
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = script,
                    WorkingDirectory = solutionLocation,
                    UseShellExecute = false
                };

                var process = new System.Diagnostics.Process
                {
                    StartInfo = processInfo
                };
                process.Start();
                process.WaitForExit();
            }
        }
    }
}
