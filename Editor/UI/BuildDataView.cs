using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.UI.Framework;

namespace Unity.ProjectAuditor.Editor.UI
{
    class BuildDataView : AnalysisView
    {
        public override string Description => m_Desc.category == IssueCategory.BuildDataSummary ?
        "Summary of asset types found in build data." :
        $"A list of {m_Desc.displayName} found in build data.";

        public BuildDataView(ViewManager viewManager) : base(viewManager)
        {
        }

        public override void Create(ViewDescriptor descriptor, IssueLayout layout, SeverityRules rules, ViewStates viewStates, IIssueFilter filter)
        {
            base.Create(descriptor, layout, rules, viewStates, filter);

            // Only for summary view, sort by size in descending order
            if (m_Desc.category == IssueCategory.BuildDataSummary)
            {
                for (int i = 0; i < layout.properties.Length; ++i)
                {
                    var layoutProperty = layout.properties[i];
                    if (layoutProperty.name == "Size")
                    {
                        m_Table.multiColumnHeader.SetSorting(i, false);
                        break;
                    }
                }
            }
        }
    }
}
