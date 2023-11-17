using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
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

        class SerializedObjectInfo
        {
            public int count;
            public long size;
        }

        Dictionary<string, SerializedObjectInfo> m_SerializedObjectInfos = new Dictionary<string, SerializedObjectInfo>();

        ProjectIssue CreateIssueForObjectType(string type, int count, long size)
        {
            return new IssueBuilder(IssueCategory.BuildDataSummary, type)
                .WithCustomProperties(
                new object[((int)BuildDataSummaryProperty.Num)]
                {
                    type,
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

                // Count all SerializedObjects
                var objects = buildAnalyzer.GetSerializedObjects<SerializedObject>();
                foreach (var obj in objects)
                {
                    var size = obj.Size;
                    if (m_SerializedObjectInfos.TryGetValue(obj.Type, out var info))
                    {
                        info.count++;
                        info.size += size;
                    }
                    else
                    {
                        m_SerializedObjectInfos.Add(obj.Type, new SerializedObjectInfo { count = 1, size = size });
                    }
                }

                // Create one issue per type of SerializedObjects
                List<ProjectIssue> issues = new List<ProjectIssue>();
                foreach (var key in m_SerializedObjectInfos.Keys)
                {
                    var issue = CreateIssueForObjectType(key, m_SerializedObjectInfos[key].count,
                        m_SerializedObjectInfos[key].size);
                    issues.Add(issue);
                }
                projectAuditorParams.OnIncomingIssues(issues);
            }

            progress?.Clear();

            projectAuditorParams.OnModuleCompleted?.Invoke();
        }
    }
}
