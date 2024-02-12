using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.Modules;

namespace Unity.ProjectAuditor.Editor.UI
{
    class BuildStepsView : BuildReportView
    {
        public override string Description => "A list of build steps and their duration.";
        public override string InfoTitle => $@"This view shows the steps involved in the last clean build of the project, and their duration.";

        public BuildStepsView(ViewManager viewManager) :
            base(viewManager)
        {
        }

        public override string GetIssueDescription(ReportItem issue)
        {
            return issue.GetCustomProperty(BuildReportStepProperty.Message);
        }
    }
}
