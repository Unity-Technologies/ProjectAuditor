using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.SettingsAnalysis;
using Unity.ProjectAuditor.Editor.Tests.Common;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.ProjectAuditor.EditorTests
{
    [Serializable]
    class RuleTests : TestFixtureBase
    {
        TestAsset m_TestAsset;

        [SerializeField]
        ProjectAuditorConfig m_SerializedConfig;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TestAsset = new TestAsset("MyClass.cs",
                "using UnityEngine; class MyClass : MonoBehaviour { void Start() { Debug.Log(Camera.allCameras.Length.ToString()); } }");
        }

#if UNITY_2019_4_OR_NEWER
        [UnityTest]
        public IEnumerator Rule_Persist_AfterDomainReload()
        {
            m_SerializedConfig = m_Config;

            m_SerializedConfig.ClearAllRules();

            Assert.AreEqual(0, m_SerializedConfig.NumRules);

            // add rule with a filter.
            m_SerializedConfig.AddRule(new Rule
            {
                id = "someid",
                severity = Severity.None
            });

            Assert.AreEqual(1, m_SerializedConfig.NumRules);

            yield return new WaitForDomainReload();

            Assert.AreEqual(1, m_SerializedConfig.NumRules);
        }

#endif

        [Test]
        public void Rule_MutedIssue_IsNotReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TestAsset);

            var allCamerasIssues = issues.Where(i => i.id == "PAC0066").ToArray();

            Assert.AreEqual(1, allCamerasIssues.Count());

            var issue = allCamerasIssues.FirstOrDefault();

            m_Config.ClearAllRules();

            var callingMethod = issue.GetContext();
            var action = m_Config.GetAction(issue.id, callingMethod);

            // expect default action specified in descriptor
            Assert.AreEqual(Severity.Default, action);

            // add rule with a filter.
            m_Config.AddRule(new Rule
            {
                id = issue.id,
                severity = Severity.None,
                filter = callingMethod
            });

            Assert.AreEqual(1, m_Config.NumRules);

            action = m_Config.GetAction(issue.id, callingMethod);

            // issue has been muted so it should not be reported
            Assert.AreEqual(Severity.None, action);
        }

#if UNITY_2019_4_OR_NEWER
        [UnityTest]
        public IEnumerator Rule_MutedIssue_IsNotReportedAfterDomainReload()
        {
            Rule_MutedIssue_IsNotReported();

            m_SerializedConfig = m_Config;
            yield return new WaitForDomainReload();
            m_Config = m_SerializedConfig; // restore config from serialized config

            Assert.AreEqual(1, m_SerializedConfig.NumRules);

            // retry after domain reload
            var issues = AnalyzeAndFindAssetIssues(m_TestAsset);

            var allCamerasIssues = issues.Where(i => i.id == "PAC0066").ToArray();

            Assert.AreEqual(1, allCamerasIssues.Count());

            var callingMethod = allCamerasIssues[0].GetContext();
            var action = m_SerializedConfig.GetAction(allCamerasIssues[0].id, callingMethod);

            // issue has been muted so it should not be reported
            Assert.AreEqual(Severity.None, action);
        }

#endif

        [Test]
        public void Rule_Test_CanBeAddedAndRemoved()
        {
            var settingsAuditor = m_ProjectAuditor.GetModule<SettingsModule>();
            var IDs = settingsAuditor.supportedDescriptorIDs;
            var config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
            var firstID = IDs.FirstOrDefault();

            Assert.IsNotNull(firstID);

            // make sure there are no rules
            var rule = config.GetRule(firstID);
            Assert.IsNull(rule);

            var filter = "dummy";

            // add rule with a filter.
            config.AddRule(new Rule
            {
                id = firstID,
                severity = Severity.None,
                filter = filter
            });

            // search for non-specific rule for this descriptor
            rule = config.GetRule(firstID);
            Assert.IsNull(rule);

            // search for specific rule
            rule = config.GetRule(firstID, filter);
            Assert.IsNotNull(rule);

            // add rule with no filter, which will replace any specific rule
            config.AddRule(new Rule
            {
                id = firstID,
                severity = Severity.None
            });

            // search for specific rule again
            rule = config.GetRule(firstID, filter);
            Assert.IsNull(rule);

            // search for non-specific rule again
            rule = config.GetRule(firstID);
            Assert.IsNotNull(rule);

            // try to delete specific rule which has been already replaced by non-specific one
            config.ClearRules(firstID, filter);

            // generic rule should still exist
            rule = config.GetRule(firstID);
            Assert.IsNotNull(rule);

            // try to delete non-specific rule
            config.ClearRules(firstID);
            rule = config.GetRule(firstID);
            Assert.IsNull(rule);

            Assert.AreEqual(0, config.NumRules);

            config.AddRule(new Rule
            {
                id = firstID,
                severity = Severity.None
            });
            Assert.AreEqual(1, config.NumRules);

            config.ClearAllRules();

            Assert.AreEqual(0, config.NumRules);
        }

        [Test]
        public void Rule_CanIgnoreSettingIssue()
        {
            var descriptorId = QualitySettingsAnalyzer.PAS1007;
            var filter = "Project/Quality/Very Low";
            var issues = Analyze(IssueCategory.ProjectSetting, i => i.id.Equals(descriptorId));

            Assert.GreaterOrEqual(issues.Length, 4);
            Assert.AreNotEqual(Severity.None, m_Config.GetAction(descriptorId));
            Assert.AreNotEqual(Severity.None, m_Config.GetAction(descriptorId, filter));

            // ignore all issues corresponding to this descriptor
            m_Config.AddRule(new Rule
            {
                id = descriptorId,
                severity = Severity.None
            });

            // TODO: once override is implemented, the issue's severity should be Severity.None
            //issues = Analyze(IssueCategory.ProjectSetting, i => i.id.Equals(descriptorId));

            Assert.AreEqual(Severity.None, m_Config.GetAction(descriptorId));
            Assert.AreEqual(Severity.None, m_Config.GetAction(descriptorId, filter));

            m_Config.ClearRules(descriptorId);

            // ignore only issues corresponding to this descriptor and filter
            m_Config.AddRule(new Rule
            {
                id = descriptorId,
                severity = Severity.None,
                filter = filter
            });

            // TODO: once override is implemented, the issue's severity should be Severity.None
            //issues = Analyze(IssueCategory.ProjectSetting, i => i.id.Equals(descriptorId));

            Assert.AreNotEqual(Severity.None, m_Config.GetAction(descriptorId));
            Assert.AreEqual(Severity.None, m_Config.GetAction(descriptorId, filter));
        }
    }
}
