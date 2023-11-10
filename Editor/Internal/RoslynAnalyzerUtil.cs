using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

// NOTE: Code files in the Editor/Internal folder are treated differently to other files, and probably shouldn't be moved from this folder.
// During the Pack process, this file (and any others in this folder) is copied into a Unity project, and ImportDILL() is called to
// pull any Roslyn Analyzer DLLs into the package.

namespace Unity.ProjectAuditor.Editor.Internal
{
    public class RoslynAnalyzerUtil
    {
        public static void ImportDLL()
        {
            var args = System.Environment.GetCommandLineArgs();

            var sourcePathKey = "-sourcePath=";
            var sourcePath = args.FirstOrDefault(arg => arg.StartsWith(sourcePathKey))?.Substring(sourcePathKey.Length);

            if (string.IsNullOrEmpty(sourcePath))
            {
                Debug.Log("Source path for the DLL(s) is not provided.");
                return;
            }

            // if not absolute path, assume it's relative to the project
            if (!Path.IsPathRooted(sourcePath))
            {
                var projectPath = Path.GetDirectoryName(Application.dataPath);
                sourcePath = Path.Combine(projectPath, sourcePath);
            }

            if (Directory.Exists(sourcePath))
            {
                foreach (var dllPath in Directory.GetFiles(sourcePath, "*.dll"))
                {
                    ProcessDll(dllPath);
                }
            }
            else if (File.Exists(sourcePath))
            {
                ProcessDll(sourcePath);
            }
            else
            {
                Debug.Log($"The path '{sourcePath}' does not exist.");
            }
        }

        static void ProcessDll(string sourcePath)
        {
            var fileName = Path.GetFileName(sourcePath);
            var pluginDir = "Assets/RoslynAnalyzers";

            // Check if the directory exists, create if it doesn't
            if (!Directory.Exists(pluginDir))
            {
                Directory.CreateDirectory(pluginDir);
            }

            var destPath = $"{pluginDir}/{fileName}";

            Debug.Log($"Process {destPath}.");

            if (File.Exists(destPath))
            {
                Debug.Log($"DLL already exists at {destPath}. Skipping.");
                return;
            }

            File.Copy(sourcePath, destPath);
            AssetDatabase.Refresh();

            var importer = (PluginImporter)AssetImporter.GetAtPath(destPath);
            if (importer != null)
            {
                importer.SetCompatibleWithAnyPlatform(false);
                importer.SetCompatibleWithEditor(false);
                importer.SaveAndReimport();

                Debug.Log($"{destPath} Asset Saved");
            }
        }
    }
}
