using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Unity.ProjectAuditor.Editor.Utils
{
    public static class PackagesUtils
    {
        public static T[] LoadPackageJson<T>(string path, string fileName) where T: class
        {
            string fullpath = Path.GetFullPath(Path.Combine(path, fileName));
            using (StreamReader r = new StreamReader(fullpath))
            {
                string packageJson = File.ReadAllText(fullpath);
                T[] packages = Json.From<T>(packageJson);
                return packages;
            }
        }


        public static string GetExtension(string url) {
            string extension = url.Substring(url.LastIndexOf("."));
            return String.IsNullOrEmpty(extension) ? "" : extension;
        }
    }
}
