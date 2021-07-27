using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Auditors
{
    class AssetsModule : ProjectAuditorModule
    {
        static readonly IssueLayout k_IssueLayout = new IssueLayout
        {
            category = IssueCategory.Asset,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Asset Name"},
                new PropertyDefinition { type = PropertyType.FileType, name = "File Type", longName = "File extension"},
                new PropertyDefinition { type = PropertyType.Path, name = "Path"}
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

        List<ProblemDescriptor> m_ProblemDescriptors;

        public override IEnumerable<ProblemDescriptor> GetDescriptors()
        {
            return m_ProblemDescriptors;
        }

        public override IEnumerable<IssueLayout> GetLayouts()
        {
            yield return k_IssueLayout;
        }

        public override void Initialize(ProjectAuditorConfig config)
        {
            m_ProblemDescriptors = new List<ProblemDescriptor>();
            RegisterDescriptor(k_Descriptor);
        }

        public override void RegisterDescriptor(ProblemDescriptor descriptor)
        {
            m_ProblemDescriptors.Add(descriptor);
        }

        public override void Audit(Action<ProjectIssue> onIssueFound, Action onComplete = null, IProgress progress = null)
        {
            AnalyzeResources(onIssueFound);
            if (onComplete != null)
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
                    Path.GetFileNameWithoutExtension(location.Path),
                    IssueCategory.Asset,
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
