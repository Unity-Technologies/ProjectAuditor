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

        public IEnumerable<ProblemDescriptor> GetDescriptors()
        {
            yield return null;
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
#if UNITY_2019_4_OR_NEWER
            if (s_BuildReport != null)
            {
                foreach (var packedAsset in s_BuildReport.packedAssets)
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
#endif
            onComplete();
        }

        public int callbackOrder { get; }
        public void OnPostprocessBuild(BuildReport report)
        {
            s_BuildReport = report;
        }
    }
}
