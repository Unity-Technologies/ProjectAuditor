using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Unity.ProjectAuditor.Editor.Auditors
{
    public class BuildAuditor : IAuditor, IPostprocessBuildWithReport
    {
        static readonly ProblemDescriptor k_Descriptor = new ProblemDescriptor
            (
            600000,
            "Build file",
            Area.BuildSize
            );

        static readonly IssueLayout k_IssueLayout = new IssueLayout
        {
            category = IssueCategory.BuildFiles,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Source Asset"},
                new PropertyDefinition { type = PropertyType.FileType, name = "Type"},
                new PropertyDefinition { type = PropertyType.Custom, format = PropertyFormat.Integer, name = "Size (bytes)", longName = "Size (bytes) in the Build"},
                new PropertyDefinition { type = PropertyType.Path, name = "Path"},
                new PropertyDefinition { type = PropertyType.Custom + 1, format = PropertyFormat.String, name = "Build File"}
            }
        };

        static BuildReport s_BuildReport;

        const string s_BuildReportDir = "Assets/BuildReports";
        const string s_LastBuildReportPath = "Library/LastBuild.buildreport";

        public IEnumerable<ProblemDescriptor> GetDescriptors()
        {
            yield return k_Descriptor;
        }

        public IEnumerable<IssueLayout> GetLayouts()
        {
            yield return k_IssueLayout;
        }

        public void Initialize(ProjectAuditorConfig config)
        {
        }

        public void RegisterDescriptor(ProblemDescriptor descriptor)
        {
        }

        public void Audit(Action<ProjectIssue> onIssueFound, Action onComplete, IProgressBar progressBar = null)
        {
//#if UNITY_2019_4_OR_NEWER
            var buildReport = GetBuildReport();
            if (buildReport != null)
            {
                foreach (var packedAsset in buildReport.packedAssets)
                {
                    var dict = new Dictionary<GUID, List<PackedAssetInfo>>();
                    foreach (var content in packedAsset.contents)
                    {
                        var assetPath = content.sourceAssetPath;
                        if (!Path.HasExtension(assetPath))
                            continue;

                        if (Path.GetExtension(assetPath).Equals(".cs"))
                            continue;

                        if (!dict.ContainsKey(content.sourceAssetGUID))
                        {
                            dict.Add(content.sourceAssetGUID, new List<PackedAssetInfo>());
                        }
                        dict[content.sourceAssetGUID].Add(content);
                    }

                    foreach (var entry in dict)
                    {
                        var content = entry.Value[0];
                        var assetPath = content.sourceAssetPath;

                        ulong sum = 0;
                        foreach (var v in entry.Value)
                        {
                            sum += v.packedSize;
                        }

                        var assetName = Path.GetFileNameWithoutExtension(assetPath);
                        string description;
                        if (entry.Value.Count > 1)
                            description = string.Format("{0} ({1})", assetName, entry.Value.Count);
                        else
                            description = assetName;
                        var issue = new ProjectIssue(k_Descriptor, description, IssueCategory.BuildFiles, new Location(assetPath));
                        issue.SetCustomProperties(new[]
                        {
                            sum.ToString(),
                            packedAsset.shortPath
                        });
                        onIssueFound(issue);
                    }
                }
            }
//#endif
            onComplete();
        }

        public static BuildReport GetBuildReport()
        {
            if (s_BuildReport != null)
                return s_BuildReport;

            if (!Directory.Exists(s_BuildReportDir))
                Directory.CreateDirectory(s_BuildReportDir);

            var date = File.GetLastWriteTime(s_LastBuildReportPath);
            var assetPath = s_BuildReportDir + "/Build_" + date.ToString("yyyy-MM-dd-HH-mm-ss") + ".buildreport";

            if (!File.Exists(assetPath))
            {
                File.Copy("Library/LastBuild.buildreport", assetPath, true);
                AssetDatabase.ImportAsset(assetPath);
            }
            s_BuildReport = AssetDatabase.LoadAssetAtPath<BuildReport>(assetPath);
            return s_BuildReport;
        }

        public int callbackOrder { get; }
        public void OnPostprocessBuild(BuildReport report)
        {
            // TODO: save
            s_BuildReport = report;
        }
    }
}
