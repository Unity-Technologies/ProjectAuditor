using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Auditors;
using UnityEngine;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public static class ScriptIssueTestHelper
    {
        public static ProjectIssue[] AnalyzeAndFindScriptIssues(TempAsset tempAsset)
        {
            var auditor = new ScriptAuditor();
            var config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
            config.AnalyzeInBackground = false;
            auditor.Initialize(config);

            var foundIssues = new List<ProjectIssue>();
            var completed = false;
            auditor.Audit(issue => {
                foundIssues.Add(issue);
            },
                () =>
                {
                    completed = true;
                });

            Assert.True(completed);

            return foundIssues.Where(i => i.relativePath.Equals(tempAsset.relativePath)).ToArray();
        }
    }
}
