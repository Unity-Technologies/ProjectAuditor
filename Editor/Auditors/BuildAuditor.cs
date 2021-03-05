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
        static readonly IssueLayout k_IssueLayout = new IssueLayout
        {
            category = IssueCategory.BuildFile,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Filename"},
                new PropertyDefinition { type = PropertyType.FileType, name = "Type"},
                new PropertyDefinition { type = PropertyType.Custom, format = PropertyFormat.Integer, name = "Size (bytes)", longName = "Size (bytes) in the Build"},
                new PropertyDefinition { type = PropertyType.Path, name = "Filename", longName = "Filename and line number"}
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

        public void Reload(string path)
        {
        }

        public void RegisterDescriptor(ProblemDescriptor descriptor)
        {
        }

        public void Audit(Action<ProjectIssue> onIssueFound, Action onComplete, IProgressBar progressBar = null)
        {
            var id = 9999;

            if (s_BuildReport != null)
            {
                foreach (var packedAsset in s_BuildReport.packedAssets)
                {
                    var descriptor = new ProblemDescriptor
                        (
                        id++,
                        packedAsset.shortPath,
                        Area.BuildSize
                        );

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
                        var issue = new ProjectIssue(descriptor, description, IssueCategory.BuildFile, new Location(assetPath));
                        issue.SetCustomProperties(new[] {sum.ToString()});
                        onIssueFound(issue);
                    }
                }
            }

            onComplete();
        }

        public int callbackOrder { get; }
        public void OnPostprocessBuild(BuildReport report)
        {
            s_BuildReport = report;
        }
    }
}
