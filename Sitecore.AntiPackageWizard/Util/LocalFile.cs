namespace Sitecore.AntiPackageWizard.Util
{
    using Sitecore.Configuration;
    using Sitecore.IO;
    using Sitecore.Extensions.StringExtensions;
    using System;
    using System.IO;

    public static class LocalFile
    {
        [NotNull]
        public static string MapPath([NotNull] string path)
        {
            path = FileUtil.NormalizeWebPath(path);

            if (path.StartsWith("web:"))
            {
                path = Combine(FileUtil.MapPath("/"), FileUtil.NormalizeWebPath(path.Mid(4)));
            }
            else if (path.StartsWith("data:"))
            {
                path = Combine(FileUtil.MapPath(Settings.DataFolder), FileUtil.NormalizeWebPath(path.Mid(5)));
            }
            else if (path.StartsWith("package:"))
            {
                path = Combine(FileUtil.MapPath(Settings.PackagePath), FileUtil.NormalizeWebPath(path.Mid(8)));
            }
            else
            {
                path = Combine(FileUtil.MapPath("/"), FileUtil.NormalizeWebPath(path));
            }

            return path;
        }

        [NotNull]
        public static string UnmapPath([NotNull] string path)
        {
            path = FileUtil.NormalizeWebPath(path);

            var dataFolder = FileUtil.NormalizeWebPath(FileUtil.MapPath(Settings.DataFolder));

            if (path.StartsWith(dataFolder, StringComparison.InvariantCultureIgnoreCase))
            {
                return path.Mid(dataFolder.Length);
            }

            return FileUtil.UnmapPath(path, false);
        }

        [NotNull]
        private static string Combine([NotNull] string path1, [NotNull] string path2)
        {
            if (path2.StartsWith("/"))
            {
                path2 = path2.Mid(1);
            }

            return Path.Combine(path1, path2);
        }
    }
}