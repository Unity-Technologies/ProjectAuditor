using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.SettingsAnalysis;
using Unity.ProjectAuditor.Editor.Tests.Common;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.ProjectAuditor.EditorTests
{
    [Serializable]
    class RuleTests : TestFixtureBase
    {
        TestAsset m_TestScriptAsset;

        [SerializeField]
        SeverityRules m_SerializedRules;

        SeverityRules m_Rules;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_TestScriptAsset = new TestAsset("MyClass.cs",
                "using UnityEngine; class MyClass : MonoBehaviour { void Start() { Debug.Log(Camera.allCameras.Length.ToString()); } }");

            m_Rules = new SeverityRules();
        }

        [UnityTest]
        public IEnumerator Rule_Persist_AfterDomainReload()
        {
            m_SerializedRules = m_Rules;

            m_SerializedRules.ClearAllRules();

            Assert.AreEqual(0, m_SerializedRules.NumRules);

            // add rule with a Filter.
            m_SerializedRules.AddRule(new Rule
            {
                Id = "ABC0001",
                Severity = Severity.None
            });

            Assert.AreEqual(1, m_SerializedRules.NumRules);

            yield return new WaitForDomainReload();

            Assert.AreEqual(1, m_SerializedRules.NumRules);
        }

        [Test]
        public void Rule_MutedIssue_IsNotReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TestScriptAsset);

            var allCamerasIssues = issues.Where(i => i.Id == "PAC0066").ToArray();

            Assert.AreEqual(1, allCamerasIssues.Count());

            var issue = allCamerasIssues.FirstOrDefault();

            m_Rules.ClearAllRules();

            var callingMethod = issue.GetContext();
            var action = m_Rules.GetAction(issue.Id, callingMethod);

            // expect default action specified in descriptor
            Assert.AreEqual(Severity.Default, action);

            // add rule with a Filter.
            m_Rules.AddRule(new Rule
            {
                Id = issue.Id,
                Severity = Severity.None,
                Filter = callingMethod
            });

            Assert.AreEqual(1, m_Rules.NumRules);

            action = m_Rules.GetAction(issue.Id, callingMethod);

            // issue has been muted so it should not be reported
            Assert.AreEqual(Severity.None, action);
        }

        [UnityTest]
        public IEnumerator Rule_MutedIssue_IsNotReportedAfterDomainReload()
        {
            Rule_MutedIssue_IsNotReported();

            m_SerializedRules = m_Rules;
            yield return new WaitForDomainReload();

            Assert.AreEqual(1, m_SerializedRules.NumRules);

            // retry after domain reload
            var issues = AnalyzeAndFindAssetIssues(m_TestScriptAsset);

            var allCamerasIssues = issues.Where(i => i.Id.Equals("PAC0066")).ToArray();

            Assert.AreEqual(1, allCamerasIssues.Count());

            var callingMethod = allCamerasIssues[0].GetContext();
            var action = m_SerializedRules.GetAction(allCamerasIssues[0].Id, callingMethod);

            // issue has been muted so it should not be reported
            Assert.AreEqual(Severity.None, action);
        }

        [Test]
        public void Rule_Test_CanBeAddedAndRemoved()
        {
            var settingsAuditor = new SettingsModule();
            var ids = settingsAuditor.SupportedDescriptorIds;
            var rules = new SeverityRules();
            var firstID = ids.FirstOrDefault();

            Assert.IsNotNull(firstID);

            // make sure there are no rules
            var rule = rules.GetRule(firstID);
            Assert.IsNull(rule);

            var filter = "dummy";

            // add rule with a Filter.
            rules.AddRule(new Rule
            {
                Id = firstID,
                Severity = Severity.None,
                Filter = filter
            });

            // search for non-specific rule for this descriptor
            rule = rules.GetRule(firstID);
            Assert.IsNull(rule);

            // search for specific rule
            rule = rules.GetRule(firstID, filter);
            Assert.IsNotNull(rule);

            // add rule with no Filter, which will replace any specific rule
            rules.AddRule(new Rule
            {
                Id = firstID,
                Severity = Severity.None
            });

            // search for specific rule again
            rule = rules.GetRule(firstID, filter);
            Assert.IsNull(rule);

            // search for non-specific rule again
            rule = rules.GetRule(firstID);
            Assert.IsNotNull(rule);

            // try to delete specific rule which has been already replaced by non-specific one
            rules.ClearRules(firstID, filter);

            // generic rule should still exist
            rule = rules.GetRule(firstID);
            Assert.IsNotNull(rule);

            // try to delete non-specific rule
            rules.ClearRules(firstID);
            rule = rules.GetRule(firstID);
            Assert.IsNull(rule);

            Assert.AreEqual(0, rules.NumRules);

            rules.AddRule(new Rule
            {
                Id = firstID,
                Severity = Severity.None
            });
            Assert.AreEqual(1, rules.NumRules);

            rules.ClearAllRules();

            Assert.AreEqual(0, rules.NumRules);
        }

        [Test]
        public void Rule_CanIgnoreSettingIssue()
        {
            var descriptorId = QualitySettingsAnalyzer.PAS1007;
            var filter = "Project/Quality/Very Low";
            var issues = Analyze(IssueCategory.ProjectSetting, i => i.Id.Equals(descriptorId));

            Assert.GreaterOrEqual(issues.Length, 4);
            Assert.AreNotEqual(Severity.None, m_Rules.GetAction(descriptorId));
            Assert.AreNotEqual(Severity.None, m_Rules.GetAction(descriptorId, filter));

            // ignore all issues corresponding to this descriptor
            m_Rules.AddRule(new Rule
            {
                Id = descriptorId,
                Severity = Severity.None
            });

            // TODO: once override is implemented, the issue's Severity should be Severity.None
            //issues = Analyze(IssueCategory.ProjectSetting, i => i.Id.Equals(descriptorId));

            Assert.AreEqual(Severity.None, m_Rules.GetAction(descriptorId));
            Assert.AreEqual(Severity.None, m_Rules.GetAction(descriptorId, filter));

            m_Rules.ClearRules(descriptorId);

            // ignore only issues corresponding to this descriptor and Filter
            m_Rules.AddRule(new Rule
            {
                Id = descriptorId,
                Severity = Severity.None,
                Filter = filter
            });

            // TODO: once override is implemented, the issue's Severity should be Severity.None
            //issues = Analyze(IssueCategory.ProjectSetting, i => i.Id.Equals(descriptorId));

            Assert.AreNotEqual(Severity.None, m_Rules.GetAction(descriptorId));
            Assert.AreEqual(Severity.None, m_Rules.GetAction(descriptorId, filter));
        }

        [Test]
        public void Rule_CanSerializeAndDeserialize()
        {
            const string k_id1 = "ABC0001";
            const string k_id2 = "ABC0002";
            const string k_filter = "Project/Quality/Very Low";

            m_Rules.ClearAllRules();

            m_Rules.AddRule(new Rule
            {
                Id = k_id1,
                Severity = Severity.None
            });

            m_Rules.AddRule(new Rule
            {
                Id = k_id2,
                Severity = Severity.Critical,
                Filter = k_filter
            });

            Assert.AreEqual(2, m_Rules.NumRules);

            var jsonString = JsonConvert.SerializeObject(m_Rules, Formatting.None,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

            Assert.NotNull(jsonString);

            m_Rules.ClearAllRules();
            Assert.AreEqual(0, m_Rules.NumRules);

            m_Rules = JsonConvert.DeserializeObject<SeverityRules>(jsonString, new JsonSerializerSettings
            {
                ObjectCreationHandling = ObjectCreationHandling.Replace
            });

            Assert.AreEqual(2, m_Rules.NumRules);

            var rule1 = m_Rules.GetRule(k_id1);
            Assert.True(rule1.Id.Equals(k_id1));
            Assert.AreEqual(rule1.Severity, Severity.None);
            Assert.AreEqual(rule1.Filter, string.Empty);

            var rule2 = m_Rules.GetRule(k_id2, k_filter);
            Assert.True(rule2.Id.Equals(k_id2));
            Assert.AreEqual(rule2.Severity, Severity.Critical);
            Assert.AreEqual(rule2.Filter, k_filter);
        }

        [Test]
        public void Rule_TemporaryRule_IsAdded()
        {
            var projectAuditorParams = new AnalysisParams { Platform = m_Platform };
            var numRules = projectAuditorParams.Rules.NumRules;

            projectAuditorParams.WithAdditionalDiagnosticRules(new List<Rule>(new[] {new Rule()}));

            Assert.AreEqual(numRules + 1, projectAuditorParams.Rules.NumRules);
        }

        [Test]
        public void Rule_TemporaryRule_DoesNotPersist()
        {
            var projectAuditorParams = new AnalysisParams { Platform = m_Platform };
            var numRules = ProjectAuditorSettings.instance.Rules.NumRules;

            projectAuditorParams.WithAdditionalDiagnosticRules(new List<Rule>(new[] {new Rule()}));

            m_ProjectAuditor.Audit();

            Assert.AreEqual(numRules, ProjectAuditorSettings.instance.Rules.NumRules);
        }
    }
}
