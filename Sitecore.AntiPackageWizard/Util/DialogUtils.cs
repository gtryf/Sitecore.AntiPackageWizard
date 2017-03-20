using Sitecore.Diagnostics;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Shell.Applications.Install;
using Sitecore.Shell.Applications.Install.Dialogs;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.HtmlControls;
using System;
using System.IO;
using Sitecore.IO;
using Sitecore.Exceptions;
using Sitecore.Globalization;

namespace Sitecore.AntiPackageWizard.Util
{
    class DialogUtils
    {
        public static JobMonitor AttachMonitor(JobMonitor monitor)
        {
            if (monitor == null)
            {
                if (!Context.ClientPage.IsEvent)
                {
                    monitor = new JobMonitor()
                    {
                        ID = "Monitor"
                    };
                    Context.ClientPage.Controls.Add(monitor);
                }
                else
                {
                    monitor = Context.ClientPage.FindControl("Monitor") as JobMonitor;
                }
            }
            return monitor;
        }

        /// <summary></summary>
        public static void Browse(ClientPipelineArgs args, Edit fileEdit)
        {
            try
            {
                DialogUtils.CheckPackageFolder();
                if (!args.IsPostBack)
                {
                    BrowseDialog.BrowseForOpen(ApplicationContext.PackagePath, "*.zip", "Choose Package", "Click the package that you want to install and then click Open.", "People/16x16/box.png");
                    args.WaitForPostBack();
                }
                else if (!args.HasResult)
                {
                    return;
                }
                else if (fileEdit != null)
                {
                    fileEdit.Value = args.Result;
                }
            }
            catch (Exception exception1)
            {
                Exception exception = exception1;
                Log.Error(exception.Message, typeof(DialogUtils));
                SheerResponse.Alert(exception.Message, new string[0]);
            }
        }

        /// <summary>Checks whether a directory for packages exists</summary>
        public static void CheckPackageFolder()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(ApplicationContext.PackagePath);
            bool flag = FileUtil.FolderExists(directoryInfo.FullName);
            bool flag1 = (directoryInfo.Parent == null ? false : FileUtil.FolderExists(directoryInfo.Parent.FullName));
            bool flag2 = FileUtil.FilePathHasInvalidChars(ApplicationContext.PackagePath);
            if (flag1 && !flag2 && !flag)
            {
                Directory.CreateDirectory(ApplicationContext.PackagePath);
                Log.Warn(string.Format("The '{0}' folder was not found and has been created. Please check your Sitecore configuration.", ApplicationContext.PackagePath), typeof(DialogUtils));
            }
            if (!Directory.Exists(ApplicationContext.PackagePath))
            {
                throw new ClientAlertException(string.Format(Translate.Text("Cannot access path '{0}'. Please check PackagePath setting in the web.config file."), ApplicationContext.PackagePath));
            }
        }

        /// <summary></summary>
        public static void Upload(ClientPipelineArgs args, Edit fileEdit)
        {
            try
            {
                DialogUtils.CheckPackageFolder();
                if (!args.IsPostBack)
                {
                    UploadPackageForm.Show(ApplicationContext.PackagePath, true);
                    args.WaitForPostBack();
                }
                else if (args.Result.StartsWith("ok:", StringComparison.InvariantCulture))
                {
                    string str = args.Result.Substring("ok:".Length);
                    string[] strArrays = str.Split(new char[] { '|' });
                    if ((int)strArrays.Length >= 1 && fileEdit != null)
                    {
                        fileEdit.Value = strArrays[0];
                    }
                }
            }
            catch (Exception exception1)
            {
                Exception exception = exception1;
                Log.Error(exception.Message, typeof(DialogUtils));
                SheerResponse.Alert(exception.Message, new string[0]);
            }
        }
    }
}