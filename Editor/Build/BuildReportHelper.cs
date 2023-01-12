using System.IO;
using Unity.ProjectAuditor.Editor.Modules;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;

namespace Unity.ProjectAuditor.Editor.Build
{
    public static class BuildReportHelper
    {
        internal const string k_LastBuildReportPath = "Library/LastBuild.buildreport";

        public static BuildReport GetLast()
        {
            if (!File.Exists(k_LastBuildReportPath))
                return null; // a build report was not found in the Library folder

            var buildReportPath = UserPreferences.buildReportPath;
            if (!Directory.Exists(buildReportPath))
                Directory.CreateDirectory(buildReportPath);

            var date = File.GetLastWriteTime(k_LastBuildReportPath);
            var targetAssetName = "Build_" + date.ToString("yyyy-MM-dd-HH-mm-ss");
            var assetPath = $"{buildReportPath}/{targetAssetName}.buildreport";

            if (!File.Exists(assetPath))
            {
                var tempAssetPath = buildReportPath + "/New Report.buildreport";
                File.Copy(k_LastBuildReportPath, tempAssetPath, true);
                AssetDatabase.ImportAsset(tempAssetPath);
                AssetDatabase.RenameAsset(tempAssetPath, targetAssetName);
            }

            return AssetDatabase.LoadAssetAtPath<UnityEditor.Build.Reporting.BuildReport>(assetPath);
        }

        [PostProcessBuild(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (UserPreferences.buildReportAutoSave)
            {
                // Library/LastBuild.buildreport is only created AFTER OnPostprocessBuild so we need to defer the copy of the file
                EditorApplication.update += CheckLastBuildReport;
            }
        }

        static void CheckLastBuildReport()
        {
            if (GetLast() != null)
                EditorApplication.update -= CheckLastBuildReport;
        }
    }

    internal class LastBuildReportProvider : BuildReportModule.IBuildReportProvider
    {
        public BuildReport GetBuildReport(BuildTarget platform)
        {
            var buildReport = BuildReportHelper.GetLast();
            if (buildReport != null && buildReport.summary.platform == platform)
                return buildReport;

            return null;
        }
    }
}
