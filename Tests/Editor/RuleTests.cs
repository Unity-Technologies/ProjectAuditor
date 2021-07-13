using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Auditors;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using UnityEngine;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    class RuleTests
    {
        TempAsset m_TempAsset;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TempAsset = new TempAsset("MyClass.cs",
                "using UnityEngine; class MyClass : MonoBehaviour { void Start() { Debug.Log(Camera.allCameras.Length.ToString()); } }");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            TempAsset.Cleanup();
        }

        [Test]
        public void ShouldNotReportMutedIssue()
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();
            var projectAuditorSettings = projectAuditor.config;
            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAsset);

            Assert.AreEqual(1, issues.Count());

            var issue = issues.FirstOrDefault();

            projectAuditorSettings.ClearAllRules();

            var callingMethod = issue.GetCallingMethod();
            var action = projectAuditorSettings.GetAction(issue.descriptor, callingMethod);

            // expect default action specified in descriptor
            Assert.AreEqual(issue.descriptor.severity, action);

            // add rule with a filter.
            projectAuditorSettings.AddRule(new Rule
            {
                id = issue.descriptor.id,
                severity = Rule.Severity.None,
                filter = callingMethod
            });

            action = projectAuditorSettings.GetAction(issue.descriptor, callingMethod);

            // issue has been muted so it should not be reported
            Assert.AreEqual(Rule.Severity.None, action);
        }

        [Test]
        public void RuleTestPass()
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();
            var settingsAuditor = projectAuditor.GetModule<SettingsModule>();
            var descriptors = settingsAuditor.GetDescriptors();
            var config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
            var firstDescriptor = descriptors.FirstOrDefault();

            // make sure there are no rules
            var rule = config.GetRule(firstDescriptor);
            Assert.IsNull(rule);

            var filter = "dummy";

            // add rule with a filter.
            config.AddRule(new Rule
            {
                id = firstDescriptor.id,
                severity = Rule.Severity.None,
                filter = filter
            });

            // search for non-specific rule for this descriptor
            rule = config.GetRule(firstDescriptor);
            Assert.IsNull(rule);

            // search for specific rule
            rule = config.GetRule(firstDescriptor, filter);
            Assert.IsNotNull(rule);

            // add rule with no filter, which will replace any specific rule
            config.AddRule(new Rule
            {
                id = firstDescriptor.id,
                severity = Rule.Severity.None
            });

            // search for specific rule again
            rule = config.GetRule(firstDescriptor, filter);
            Assert.IsNull(rule);

            // search for non-specific rule again
            rule = config.GetRule(firstDescriptor);
            Assert.IsNotNull(rule);

            // try to delete specific rule which has been already replaced by non-specific one
            config.ClearRules(firstDescriptor, filter);

            // generic rule should still exist
            rule = config.GetRule(firstDescriptor);
            Assert.IsNotNull(rule);

            // try to delete non-specific rule
            config.ClearRules(firstDescriptor);
            rule = config.GetRule(firstDescriptor);
            Assert.IsNull(rule);

            Assert.AreEqual(0, config.NumRules);

            config.AddRule(new Rule
            {
                id = firstDescriptor.id,
                severity = Rule.Severity.None
            });
            Assert.AreEqual(1, config.NumRules);

            config.ClearAllRules();

            Assert.AreEqual(0, config.NumRules);
        }
    }
}
