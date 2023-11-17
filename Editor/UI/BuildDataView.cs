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

        public override void Clear()
        {
            base.Clear();

            if (m_Desc.category == IssueCategory.BuildDataSummary)
            {
                var header = m_Table.multiColumnHeader;

                var layout = m_Layout;

                for (int i = 0; i < layout.properties.Length; ++i)
                {
                    var layoutProperty = layout.properties[i];
                    if (layoutProperty.name == "Size")
                    {
                        header.SetSorting(i, false);
                        break;
                    }
                }
            }
        }
    }
}
