using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Tests.Common;
using Unity.ProjectAuditor.Editor.UI;
using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_2020_1_OR_NEWER

namespace Unity.ProjectAuditor.EditorTests
{
    class AnalysisViewTests : TestFixtureBase
    {
        private ProjectAuditorWindow m_PaWindow => ProjectAuditorWindow.Instance;
        private bool m_IsWindowOpen = false;
        private const string m_BuildSizeMsg = "A list of files contributing to the build size.";
        private const string m_Path = "project-auditor-report.json";

        [UnitySetUp]
        public IEnumerator Setup()
        {
            if (!m_IsWindowOpen)
            {
                m_PaWindow.Show();
                m_PaWindow.Focus();
                m_PaWindow.Repaint();

                var flagsValue = ProjectAreaFlags.All; //1 << 0 | 1 << 1 | 1 << 2 | 1 << 3 | 1 << 4;
#if PA_WELCOME_VIEW_OPTIONS
                m_PaWindow.GetType()
                    .GetField("m_SelectedProjectAreas", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(m_PaWindow, flagsValue);
#else
                UserPreferences.ProjectAreasToAnalyze = flagsValue;
#endif
                MethodInfo method = ProjectAuditorWindow.Instance.GetType().GetMethod("Analyze",

                    BindingFlags.NonPublic | BindingFlags.Instance);
                method.Invoke(m_PaWindow, null);

                EditorUtility.RequestScriptReload();
                yield return new WaitForDomainReload();
            }

            m_IsWindowOpen = true;
        }

        [Order(0)]
        [Test]
        public void SummaryView_Displays_ReportInformation()
        {
            var report = (Report)m_PaWindow.GetType().
                GetField("m_Report", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(m_PaWindow);

            Assert.NotNull(report);

            var numOfIssues = report.NumTotalIssues;
            Assert.Greater(numOfIssues, 0);

            Assert.AreEqual(Application.unityVersion, report.SessionInfo.UnityVersion);
            Assert.AreEqual(Application.companyName, report.SessionInfo.CompanyName);
            Assert.AreEqual(Application.productName, report.SessionInfo.ProjectName);
            Assert.AreEqual(ProjectAuditorPackage.Version, report.SessionInfo.ProjectAuditorVersion);
            Assert.AreEqual(Application.unityVersion, report.SessionInfo.UnityVersion);
        }

        [Order(1)]
        [Test]
        public void ChangeView_SuccessfullyChangesDisplayedCategory()
        {
            var viewManager = (ViewManager)m_PaWindow.GetType().
                GetField("m_ViewManager", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(m_PaWindow);
            Assert.NotNull(viewManager);

            viewManager.ChangeView(IssueCategory.Code);
            viewManager.ChangeView(IssueCategory.BuildSummary);
            viewManager.ChangeView(IssueCategory.BuildFile);

            BuildReportView buildView = viewManager.GetActiveView() as BuildReportView;

            viewManager.ChangeView(IssueCategory.BuildStep);
            Assert.AreEqual(buildView.Description, m_BuildSizeMsg);

            viewManager.ChangeView(IssueCategory.AssetIssue);
            viewManager.ChangeView(IssueCategory.Shader);

            Assert.NotNull(viewManager);
        }

        [Order(2)]
        [Test]
        public void Reports_CanBeSavedAndLoaded()
        {
            var report = (Report)m_PaWindow.GetType().
                GetField("m_Report", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(m_PaWindow);

            report.Save(m_Path);
            var loadedReport = Report.Load(m_Path);

            Assert.AreEqual(report.NumTotalIssues, loadedReport.NumTotalIssues);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            m_PaWindow.Close();
        }
    }
}
#endif
