using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Auditors
{
    public class AssetsAuditor : IAuditor
    {
        private static readonly ProblemDescriptor s_Descriptor = new ProblemDescriptor
            (
            302000,
            "Resources folder asset",
            Area.BuildSize,
            "The Resources folder is a common source of many problems in Unity projects. Improper use of the Resources folder can bloat the size of a project’s build, lead to uncontrollable excessive memory utilization, and significantly increase application startup times.",
            "Use AssetBundles when possible"
            );

        private List<ProblemDescriptor> m_ProblemDescriptors = new List<ProblemDescriptor>();

        public IEnumerable<ProblemDescriptor> GetDescriptors()
        {
            return m_ProblemDescriptors;
        }

        public void Initialize(ProjectAuditorConfig config)
        {
            RegisterDescriptor(s_Descriptor);
        }

        public void Reload(string path)
        {
        }

        public void RegisterDescriptor(ProblemDescriptor descriptor)
        {
            m_ProblemDescriptors.Add(descriptor);
        }

        public void Audit(Action<ProjectIssue> onIssueFound, Action onComplete, IProgressBar progressBar = null)
        {
            AnalyzeResources(onIssueFound);
            onComplete();
        }

        private static void AnalyzeResources(Action<ProjectIssue> onIssueFound)
        {
            var allAssetPaths = AssetDatabase.GetAllAssetPaths();
            var allResources = allAssetPaths.Where(path => path.IndexOf("/resources/", StringComparison.OrdinalIgnoreCase) >= 0);
            var allPlayerResources = allResources.Where(path => path.IndexOf("/editor/", StringComparison.OrdinalIgnoreCase) == -1);

            var resourceAssetPathsHashSet = new HashSet<string>();
            foreach (var assetPath in allPlayerResources)
            {
                if ((File.GetAttributes(assetPath) & FileAttributes.Directory) == FileAttributes.Directory)
                    continue;

                // get all dependencies, including 'assetPath'
                var dependencies = AssetDatabase.GetDependencies(assetPath, true);
                foreach (var dep in dependencies)
                {
                    // skip C# scripts
                    if (Path.GetExtension(dep).Equals(".cs"))
                        continue;

                    resourceAssetPathsHashSet.Add(dep);
                }
            }

            foreach (var assetPath in resourceAssetPathsHashSet)
            {
                var location = new Location(assetPath, LocationType.Asset);
                onIssueFound(new ProjectIssue
                    (
                        s_Descriptor,
                        location.Path,
                        IssueCategory.Assets,
                        location
                    )
                );
            }
        }
    }
}
