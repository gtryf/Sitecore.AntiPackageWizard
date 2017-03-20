using Sitecore.AntiPackageWizard.Util;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Install;
using Sitecore.Install.Framework;
using Sitecore.Install.Metadata;
using Sitecore.Install.Zip;
using Sitecore.IO;
using Sitecore.Jobs;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using System;
using System.IO;
using System.Threading;

namespace Sitecore.AntiPackageWizard.Dialogs.CreateAntiPackage
{
    public class CreateAntiPackageForm : WizardForm
    {
        protected Edit PackageFile;

        /// <summary></summary>
        protected Edit PackageName;

        /// <summary></summary>
        protected Edit Version;

        /// <summary></summary>
        protected Edit Author;

        /// <summary></summary>
        protected Edit Publisher;

        /// <summary></summary>
        protected JobMonitor Monitor;

        /// <summary></summary>
        /// <summary></summary>
        protected Border SuccessMessage;

        /// <summary></summary>
        protected Border FailureMessage;

        protected string ResultFile
        {
            get { return StringUtil.GetString(Context.ClientPage.ServerProperties["result-file"]); }
            set { Context.ClientPage.ServerProperties["result-file"] = value; }
        }

        private string OriginalNextButtonHeader
        {
            get { return StringUtil.GetString(Context.ClientPage.ServerProperties["next-header"]); }
            set { Context.ClientPage.ServerProperties["next-header"] = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this installation is successful.
        /// </summary>
        /// <value><c>true</c> if successful; otherwise, <c>false</c>.</value>
        private bool Successful
        {
            get
            {
                object item = base.ServerProperties["Successful"];
                if (!(item is bool))
                {
                    return true;
                }
                return (bool)item;
            }
            set { base.ServerProperties["Successful"] = value; }
        }

        private bool LoadPackage()
        {
            string value = this.PackageFile.Value;
            if (Path.GetExtension(value).Trim().Length == 0)
            {
                value = Path.ChangeExtension(value, ".zip");
                this.PackageFile.Value = value;
            }
            if (value.Trim().Length == 0)
            {
                Context.ClientPage.ClientResponse.Alert("Please specify a package.");
                return false;
            }
            value = Installer.GetFilename(value);
            if (!FileUtil.FileExists(value))
            {
                Context.ClientPage.ClientResponse.Alert(Translate.Text("The package \"{0}\" file does not exist.", new[] { value }));
                return false;
            }
            IProcessingContext processingContext = Installer.CreatePreviewContext();
            ISource<PackageEntry> packageReader = new PackageReader(MainUtil.MapPath(value));
            MetadataView metadataView = new MetadataView(processingContext);
            MetadataSink metadataSink = new MetadataSink(metadataView);
            metadataSink.Initialize(processingContext);
            packageReader.Populate(metadataSink);
            if (processingContext == null || processingContext.Data == null)
            {
                Context.ClientPage.ClientResponse.Alert(Translate.Text("The package \"{0}\" could not be loaded.\n\nThe file maybe corrupt.", new[] { value }));
                return false;
            }
            this.ResultFile = new Util.CreateAntiPackage().GetTargetFileName(value);
            return true;
        }

        /// <summary>
        /// Called when the active page is changing.
        /// </summary>
        /// <param name="page">The page that is being left.</param>
        /// <param name="newpage">The new page that is being entered.</param>
        protected override bool ActivePageChanging(string page, ref string newpage)
        {
            bool flag = base.ActivePageChanging(page, ref newpage);
            if (page == "LoadPackage")
            {
                flag = this.LoadPackage();
            }
            
            return flag;
        }

        /// <summary>
        /// Called when the active page has been changed.
        /// </summary>
        /// <param name="page">The page that has been entered.</param>
        /// <param name="oldPage">The page that was left.</param>
        protected override void ActivePageChanged(string page, string oldPage)
        {
            base.ActivePageChanged(page, oldPage);
            this.NextButton.Header = this.OriginalNextButtonHeader;
            if (page == "Installing")
            {
                this.BackButton.Disabled = true;
                this.NextButton.Disabled = true;
                this.CancelButton.Disabled = true;
                Context.ClientPage.SendMessage(this, "antipackage:create");
            }
            if (page == "Ready")
            {
                this.NextButton.Header = Translate.Text("Create");
            }
            if (page == "LastPage")
            {
                this.BackButton.Disabled = true;
            }
            if (!this.Successful)
            {
                this.CancelButton.Header = Translate.Text("Close");
                this.Successful = true;
            }
        }

        /// <summary></summary>
        [HandleMessage("antipackage:browse", true)]
        protected void Browse(ClientPipelineArgs args)
        {
            DialogUtils.Browse(args, this.PackageFile);
        }

        /// <summary></summary>
        protected override void EndWizard()
        {
            Sitecore.Shell.Framework.Windows.Close();
        }

        private static string GetFullDescription(Exception e) => e.ToString();

        private static string GetShortDescription(Exception e)
        {
            string message = e.Message;
            int num = message.IndexOf("(method:", StringComparison.InvariantCulture);
            if (num <= -1)
            {
                return message;
            }
            return message.Substring(0, num - 1);
        }

        private void ShowErrorMessage(string shortDescription)
        {
            CreateAntiPackageForm.SetVisibility(this.SuccessMessage, false);
            CreateAntiPackageForm.SetVisibility(this.FailureMessage, true);
            this.Successful = false;
            Context.ClientPage.ClientResponse.SetInnerHtml("FailureMessage",
                Translate.Text("Anti-package generation failed: {0}.", new[] { shortDescription }));
            base.Active = "LastPage";
        }

        private void Monitor_JobFinished(object sender, EventArgs e)
        {
            base.Next();
        }

        /// <summary></summary>
        protected override void OnCancel(object sender, EventArgs formEventArgs)
        {
            this.Cancel();
        }

        [HandleMessage("antipackage:aborted")]
        protected void OnGeneratorAborted(Message message)
        {
            this.ShowErrorMessage(Translate.Text("Anti-package generation was aborted"));
        }

        /// <summary></summary>
        [HandleMessage("antipackage:failed")]
        protected void OnGeneratorFailed(Message message)
        {
            Job job = JobManager.GetJob(this.Monitor.JobHandle);
            Assert.IsNotNull(job, "Job is not available");
            Exception result = job.Status.Result as Exception;
            Error.AssertNotNull(result, "Cannot get any exception details");
            this.ShowErrorMessage(CreateAntiPackageForm.GetShortDescription(result));
        }

        /// <summary>
        /// Shows a download dialog.
        /// </summary>
        /// <param name="message">The message.</param>
        [HandleMessage("antipackage:download")]
        protected void DownloadPackage(Message message)
        {
            string resultFile = this.ResultFile;
            if (resultFile.Length > 0)
            {
                Context.ClientPage.ClientResponse.Download(resultFile);
                return;
            }
            Context.ClientPage.ClientResponse.Alert("Could not download package");
        }

        /// <summary></summary>
        protected override void OnLoad(EventArgs e)
        {
            if (!Context.ClientPage.IsEvent)
            {
                this.OriginalNextButtonHeader = this.NextButton.Header;
            }
            base.OnLoad(e);
            this.Monitor = DialogUtils.AttachMonitor(this.Monitor);
            if (!Context.ClientPage.IsEvent)
            {
                this.PackageFile.Value = Registry.GetString("Packager/File");
            }
            this.Monitor.JobFinished += new EventHandler(this.Monitor_JobFinished);
            base.WizardCloseConfirmationText = "Are you sure you want to cancel creating an anti-package?";
        }

        private static void SetVisibility(Control control, bool visible)
        {
            Context.ClientPage.ClientResponse.SetStyle(control.ID, "display", (visible ? "" : "none"));
        }

        /// <summary>
        /// Starts the installation.
        /// </summary>
        /// <param name="message">The message.</param>
        [HandleMessage("antipackage:create")]
        protected void StartInstallation(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            string filename = Installer.GetFilename(this.PackageFile.Value);
            if (FileUtil.IsFile(filename))
            {
                this.StartTask(filename);
                return;
            }
            Context.ClientPage.ClientResponse.Alert("Package not found");
            base.Active = "Ready";
            this.BackButton.Disabled = true;
        }

        private void StartTask(string packageFile)
        {
            this.Monitor.Start("Antipackage", "Install", new ThreadStart(new AsyncHelper(packageFile).Create));
        }

        /// <summary></summary>
        [HandleMessage("antipackage:upload", true)]
        protected void Upload(ClientPipelineArgs args)
        {
            DialogUtils.Upload(args, this.PackageFile);
        }

        private class AsyncHelper
        {
            private string _packageFile;
            
            public AsyncHelper(string package)
            {
                this._packageFile = package;
            }

            private void CatchExceptions(ThreadStart start)
            {
                try
                {
                    start();
                }
                catch (ThreadAbortException)
                {
                    if (!Environment.HasShutdownStarted)
                    {
                        Thread.ResetAbort();
                    }
                    Log.Info("Anti-package generation was aborted", this);
                    JobContext.PostMessage("antipackage:aborted");
                    JobContext.Flush();
                }
                catch (Exception ex)
                {
                    Log.Error(string.Concat("Anti-package generation failed: ", ex), this);
                    JobContext.Job.Status.Result = ex;
                    JobContext.PostMessage("antipackage:failed");
                    JobContext.Flush();
                }
            }

            /// <summary>
            /// Performs installation.
            /// </summary>
            public void Create()
            {
                this.CatchExceptions(() =>
                {
                    var antipackageCreator = new Util.CreateAntiPackage();
                    antipackageCreator.Execute(this._packageFile);
                });
            }
        }
    }
}