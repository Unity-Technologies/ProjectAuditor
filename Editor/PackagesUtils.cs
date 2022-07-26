using System;
using System.IO;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor.PackageManager.Requests;

namespace Unity.ProjectAuditor.Editor
{
    public static class PackagesUtils
    {

        public static AddRequest Request;

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
