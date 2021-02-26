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
        static readonly IssueLayout k_IssueLayout = new IssueLayout
        {
            category = IssueCategory.Assets,
            properties = new []
            {
                new IssueProperty { type = PropertyType.Description, name = "Asset Name"},
                new IssueProperty { type = PropertyType.FileType, name = "File Type", longName = "File extension"},
                new IssueProperty { type = PropertyType.Path, name = "Path", longName = "Path"}
            }
        };

        static readonly ProblemDescriptor k_Descriptor = new ProblemDescriptor
            (
            302000,
            "Resources folder asset & dependencies",
            Area.BuildSize,
            "The Resources folder is a common source of many problems in Unity projects. Improper use of the Resources folder can bloat the size of a project’s build, lead to uncontrollable excessive memory utilization, and significantly increase application startup times.",
            "Use AssetBundles when possible"
            );

        readonly List<ProblemDescriptor> m_ProblemDescriptors = new List<ProblemDescriptor>();

        public IEnumerable<ProblemDescriptor> GetDescriptors()
        {
            return m_ProblemDescriptors;
        }

        public IEnumerable<IssueLayout> GetLayouts()
        {
            yield return k_IssueLayout;
        }

        public void Initialize(ProjectAuditorConfig config)
        {
            RegisterDescriptor(k_Descriptor);
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

        static void AnalyzeResources(Action<ProjectIssue> onIssueFound)
        {
            var allAssetPaths = AssetDatabase.GetAllAssetPaths();
            var allResources = allAssetPaths.Where(path => path.IndexOf("/resources/", StringComparison.OrdinalIgnoreCase) >= 0);
            var allPlayerResources = allResources.Where(path => path.IndexOf("/editor/", StringComparison.OrdinalIgnoreCase) == -1);

            var assetPathsDict = new Dictionary<string, DependencyNode>();
            foreach (var assetPath in allPlayerResources)
            {
                if ((File.GetAttributes(assetPath) & FileAttributes.Directory) == FileAttributes.Directory)
                    continue;

                var root = AddResourceAsset(assetPath, assetPathsDict, onIssueFound, null);
                var dependencies = AssetDatabase.GetDependencies(assetPath, true);
                foreach (var depAssetPath in dependencies)
                {
                    // skip self
                    if (depAssetPath.Equals(assetPath))
                        continue;

                    AddResourceAsset(depAssetPath, assetPathsDict, onIssueFound, root);
                }
            }
        }

        static DependencyNode AddResourceAsset(
            string assetPath, Dictionary<string, DependencyNode> assetPathsDict, Action<ProjectIssue> onIssueFound, DependencyNode parent)
        {
            // skip C# scripts
            if (Path.GetExtension(assetPath).Equals(".cs"))
                return null;

            if (assetPathsDict.ContainsKey(assetPath))
            {
                var dep = assetPathsDict[assetPath];
                if (parent != null)
                    dep.AddChild(parent);
                return dep;
            }

            var location = new Location(assetPath);
            var dependencyNode = new AssetDependencyNode
            {
                location = new Location(assetPath)
            };
            if (parent != null)
                dependencyNode.AddChild(parent);

            onIssueFound(new ProjectIssue
                (
                    k_Descriptor,
                    k_IssueLayout,
                    Path.GetFileNameWithoutExtension(location.Path),
                    IssueCategory.Assets,
                    location
                )
                {
                    dependencies = dependencyNode
                }
            );

            assetPathsDict.Add(assetPath, dependencyNode);

            return dependencyNode;
        }
    }
}
