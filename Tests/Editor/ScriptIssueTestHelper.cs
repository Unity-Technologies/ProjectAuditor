using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Auditors;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public static class ScriptIssueTestHelper
    {
        public static IEnumerable<ProjectIssue> AnalyzeAndFindScriptIssues(ScriptResource scriptResource)
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();
            var projectReport = projectAuditor.Audit();
            return ScriptAuditor.FindScriptIssues(projectReport, scriptResource.relativePath);
        }
    }
}