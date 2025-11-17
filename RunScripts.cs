using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT_PrebuildScripts
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class RunScripts
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4130;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("d70b81c5-3bdb-408f-a88d-d1c5c21bd366");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="RunScripts"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private RunScripts(AsyncPackage package, OleMenuCommandService commandService)
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
        public static RunScripts Instance
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
            // Switch to the main thread - the call to AddCommand in Command1's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new RunScripts(package, commandService);
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

            //get the pre scripts from file
            string preScriptsPath = solutionLocation + "\\pre_build_scripts.txt";
            List<string> preScripts = new List<string>();
            if (File.Exists(preScriptsPath)) {
                using (StreamReader reader = new StreamReader(preScriptsPath)) {
                    string line;
                    while ((line = reader.ReadLine()) != null) {
                        preScripts.Add(line);
                    }
                }
            }

            //get the post scripts from file
            List<string> postScripts = new List<string>();
            string postScriptsPath = solutionLocation + "\\post_build_scripts.txt";
            if (File.Exists(postScriptsPath)) {
                using (StreamReader reader = new StreamReader(postScriptsPath)) {
                    string line;
                    while ((line = reader.ReadLine()) != null) {
                        postScripts.Add(line);
                    }
                }
            }

            //execute the pre scripts
            foreach (string script in preScripts)
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

            //trigger clean and build
            solutionBuild.Clean(true);
            solutionBuild.Build(true);

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
