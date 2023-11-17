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
    }
}
