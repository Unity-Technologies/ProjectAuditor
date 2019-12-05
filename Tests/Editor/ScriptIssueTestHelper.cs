using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public static class ScriptIssueTestHelper
    {
        static public IEnumerable<ProjectIssue> AnalyzeAndFindScriptIssues(string relativePath)
        {
            var projectReport = new ProjectReport();
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();

            projectAuditor.Audit(projectReport);
            return ScriptAuditor.FindScriptIssues(projectReport, relativePath);
        }
    }
}