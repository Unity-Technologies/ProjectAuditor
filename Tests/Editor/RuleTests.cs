using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.ProjectAuditor.EditorTests
{
    [Serializable]
    class RuleTests : TestFixtureBase
    {
        TempAsset m_TempAsset;

        [SerializeField]
        ProjectAuditorConfig m_SerializedConfig;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TempAsset = new TempAsset("MyClass.cs",
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
            var issues = AnalyzeAndFindAssetIssues(m_TempAsset);

            Assert.AreEqual(1, issues.Count());

            var issue = issues.FirstOrDefault();

            m_Config.ClearAllRules();

            var callingMethod = issue.GetContext();
            var action = m_Config.GetAction(issue.descriptor, callingMethod);

            // expect default action specified in descriptor
            Assert.AreEqual(issue.descriptor.severity, action);

            // add rule with a filter.
            m_Config.AddRule(new Rule
            {
                id = issue.descriptor.id,
                severity = Severity.None,
                filter = callingMethod
            });

            Assert.AreEqual(1, m_Config.NumRules);

            action = m_Config.GetAction(issue.descriptor, callingMethod);

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
            var issues = AnalyzeAndFindAssetIssues(m_TempAsset);

            var callingMethod = issues[0].GetContext();
            var action = m_SerializedConfig.GetAction(issues[0].descriptor, callingMethod);

            // issue has been muted so it should not be reported
            Assert.AreEqual(Severity.None, action);
        }

#endif

        [Test]
        public void Rule_Test_CanBeAddedAndRemoved()
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();
            var settingsAuditor = projectAuditor.GetModule<SettingsModule>();
            var descriptors = settingsAuditor.supportedDescriptors;
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
                severity = Severity.None,
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
                severity = Severity.None
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
                severity = Severity.None
            });
            Assert.AreEqual(1, config.NumRules);

            config.ClearAllRules();

            Assert.AreEqual(0, config.NumRules);
        }
    }
}
