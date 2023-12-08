using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.UI.Framework;

namespace Unity.ProjectAuditor.Editor.UI
{
    class BuildDataView : AnalysisView
    {
        private bool m_DirtySorting;
        private bool m_ExpandOnSelection;

        public override string Description => m_Desc.category == IssueCategory.BuildDataSummary ?
        "Summary of asset types found in build data." :
        $"A list of {m_Desc.displayName} found in build data.";

        public BuildDataView(ViewManager viewManager) : base(viewManager)
        {
        }

        public override void Create(ViewDescriptor descriptor, IssueLayout layout, SeverityRules rules, ViewStates viewStates, IIssueFilter filter)
        {
            base.Create(descriptor, layout, rules, viewStates, filter);

            m_DirtySorting = true;
            m_ExpandOnSelection = true;

            m_Table.isForcedGroup = true;
        }

        public override void Clear()
        {
            base.Clear();

            m_DirtySorting = true;
            m_ExpandOnSelection = true;
        }

        public override void DrawContent(bool showDetails = false)
        {
            if (m_DirtySorting)
            {
                InitSorting();
                m_DirtySorting = false;
            }

            if (m_ExpandOnSelection && GetSelection().Length > 0)
            {
                DependencyView?.ExpandAll();
            }

            base.DrawContent(showDetails);
        }

        private void InitSorting()
        {
            // Only for summary view, sort by size in descending order
            if (m_Desc.category == IssueCategory.BuildDataSummary)
            {
                var sizePropertyType = PropertyTypeUtil.FromCustom(BuildDataSummaryProperty.Size);
                var typePropertyType = PropertyTypeUtil.FromCustom(BuildDataSummaryProperty.Type);

                for (int i = 0; i < m_Layout.properties.Length; ++i)
                {
                    var layoutProperty = m_Layout.properties[i];
                    if (layoutProperty.type == sizePropertyType)
                    {
                        m_Table.multiColumnHeader.SetSorting(i, false);
                    }
                    else if (layoutProperty.type == typePropertyType)
                    {
                        m_Table.groupPropertyIndex = i;
                    }
                }
            }
            else if (m_Desc.category == IssueCategory.BuildDataShader)
            {
                for (int i = 0; i < m_Layout.properties.Length; ++i)
                {
                    var layoutProperty = m_Layout.properties[i];
                    if (layoutProperty.name == "Decompressed Size")
                    {
                        m_Table.multiColumnHeader.SetSorting(i, false);
                        break;
                    }
                }
            }
        }
    }
}
