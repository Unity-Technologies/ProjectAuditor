using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.BuildData;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.BuildData.SerializedObjects;

namespace Unity.ProjectAuditor.Editor.Modules
{
    enum BuildDataSummaryProperty
    {
        Type,
        Count,
        Size,
        Num,
    }

    class BuildDataSummaryModule : Module
    {
        static readonly IssueLayout k_SummaryLayout = new IssueLayout
        {
            category = IssueCategory.BuildDataSummary,
            properties = new[]
            {
                // new PropertyDefinition { type = PropertyType.Description, format = PropertyFormat.String, name = "Name", longName = "Shader Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataSummaryProperty.Type), format = PropertyFormat.String, name = "Type", longName = "Asset Type" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataSummaryProperty.Count), format = PropertyFormat.Integer, name = "Count", longName = "Number Of Assets Of This Type" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataSummaryProperty.Size), format = PropertyFormat.Bytes, name = "Size", longName = "Size In Bytes" },
            }
        };

        public override string Name => "Summary";

        public override bool IsEnabledByDefault => false;

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_SummaryLayout
        };

        ProjectIssue CreateIssueForSerializedObjects<T>(Analyzer buildAnalyzer) where T : SerializedObject
        {
            int count = 0;
            long size = 0;

            var objects = buildAnalyzer.GetSerializedObjects<T>();
            foreach (var obj in objects)
            {
                count++;
                size += obj.Size;
            }

            var name = typeof(T).Name;

            return new IssueBuilder(IssueCategory.BuildDataSummary, name)
                .WithCustomProperties(
                new object[((int)BuildDataSummaryProperty.Num)]
                {
                    name,
                    count,
                    size
                });
        }

        public override void Audit(AnalysisParams projectAuditorParams, IProgress progress = null)
        {
            if (projectAuditorParams.BuildAnalyzer != null)
            {
                var buildAnalyzer = projectAuditorParams.BuildAnalyzer;

                progress?.Start("Parsing all assets from Build Data", "Search in Progress...", 1);

                List<ProjectIssue> issues = new List<ProjectIssue>();

                issues.Add(CreateIssueForSerializedObjects<Texture2D>(buildAnalyzer));
                issues.Add(CreateIssueForSerializedObjects<Mesh>(buildAnalyzer));
                issues.Add(CreateIssueForSerializedObjects<AnimationClip>(buildAnalyzer));
                issues.Add(CreateIssueForSerializedObjects<AudioClip>(buildAnalyzer));
                issues.Add(CreateIssueForSerializedObjects<Shader>(buildAnalyzer));

                projectAuditorParams.OnIncomingIssues(issues);
            }

            progress?.Clear();

            projectAuditorParams.OnModuleCompleted?.Invoke();
        }
    }
}
