using System;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor.PackageManager.Requests;

namespace Unity.ProjectAuditor.Editor
{


    public static class PackagesUtils
    {
        [Serializable]
        public class Dependency
        {
            public string name;
            public string version;
        }
        [Serializable]
        public class PackageItem
        {
            public string name;
            public string description;
            public string url;
            public Dependency[] dependencies;
            public string publishDate;
            public string version;
            public string unity;
        }

        public static AddRequest Request;
        public static PackageItem[] packages;

        public static void Initial(string dataPath, string fileName) {
            packages = LoadPackageJson<PackageItem>(dataPath, fileName);
        }

        public static string[] GetPackagesNames() {
            if (packages.Length != 0) {
                string[] options = new string[packages.Length + 1];
                for (int i = 0; i < options.Length; i++)
                {
                    if (i == 0)
                    {
                        options[i] = "Please select...";
                    }
                    else {
                        options[i] = packages[i - 1].description;
                    }
                }
                return options;
            }
            else
                return null;
        }

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

        public static string DownloadFile(string url, string fileName, IProgress progressBar) {
            string path = Path.Combine(UnityEngine.Application.persistentDataPath, fileName + PackagesUtils.GetExtension(url));
            System.Net.WebClient myWebClient = new System.Net.WebClient();
            progressBar.Start("Download Package", "Downloading Package", int.MaxValue);
            progressBar.Advance();
            myWebClient.DownloadFile(url, path);
            progressBar.Clear();
            return path;
        }


        public static void InstallPackage(int index, IProgress progressBar)
        {
            if (packages[index - 1].dependencies.Length != 0)
            {
                foreach (var dependecy in packages[index - 1].dependencies)
                {
                    var package = packages.Where(p => (dependecy.name == p.name && dependecy.version == p.version)).ToArray();
                    InstallPackage(DownloadFile(package[0].url, package[0].name, progressBar), progressBar);
                }
            }
            PackagesUtils.InstallPackage(PackagesUtils.DownloadFile(PackagesUtils.packages[index - 1].url, PackagesUtils.packages[index - 1].name, progressBar), progressBar);
        }

        public static void InstallPackage(string path, IProgress progressBar) {
            string fileFullPath = "file:" + path;
            progressBar.Start("Install Package", "Installing Package", int.MaxValue);
            Request = UnityEditor.PackageManager.Client.Add(fileFullPath);
            UnityEditor.EditorApplication.update += Progress;
            progressBar.Advance();
            while (Request.Status == UnityEditor.PackageManager.StatusCode.InProgress) { }
            if (Request.Status == UnityEditor.PackageManager.StatusCode.Success)
                progressBar.Clear();
        }

        private static void Progress()
        {
            if (Request.IsCompleted)
            {
                if (Request.Status == UnityEditor.PackageManager.StatusCode.Success)
                    UnityEngine.Debug.Log("Installed: " + Request.Result.packageId);
                else if (Request.Status >= UnityEditor.PackageManager.StatusCode.Failure)
                    UnityEngine.Debug.Log(Request.Error.message);

                UnityEditor.EditorApplication.update -= Progress;

            }
        }
    }
}
