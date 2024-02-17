using System;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Tests.Common;
using Unity.ProjectAuditor.Editor.UI.Framework;

namespace Unity.ProjectAuditor.EditorTests
{
    class ViewManagerTests : TestFixtureBase
    {
        TestAsset m_TestScriptAsset;

        ViewManager m_ViewManager;
        IssueCategory[] m_TestCategories;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_TestScriptAsset = new TestAsset("MyClass.cs",
                "using UnityEngine; class MyClass : MonoBehaviour { void Start() { Debug.Log(Camera.allCameras.Length.ToString()); } }");

            // Setup test data
            m_TestCategories = new[] { IssueCategory.Code, IssueCategory.ProjectSetting };
            AnalyzeTestAssets();

            // Instantiate the ViewManager
            m_ViewManager = new ViewManager(m_TestCategories);
            m_ViewManager.Create(m_AnalysisParams.Rules, new ViewStates());
        }

        [Test]
        public void UI_ViewManager_Constructor_SetsCategoriesCorrectly()
        {
            Assert.AreEqual(m_TestCategories.Length, m_ViewManager.NumViews);
        }

        [Test]
        public void UI_ViewManager_AddIssues_AddsIssuesToViews()
        {
            // Add issues to the view manager
            m_ViewManager.AddIssues(GetIssues());

            // Check each view for added issues
            for (var i = 0; i < m_ViewManager.NumViews; i++)
            {
                var view = m_ViewManager.GetView(i);
                Assert.Positive(view.NumIssues, $"View {view.Desc.Category} has no issues");
            }
        }

        [Test]
        public void UI_ViewManager_Clear_ClearsAllViews()
        {
            // Add issues to the view manager
            m_ViewManager.AddIssues(GetIssues());

            // Clear all views
            m_ViewManager.Clear();

            // Check each view to ensure it is cleared
            for (var i = 0; i < m_ViewManager.NumViews; i++)
            {
                var view = m_ViewManager.GetView(i);
                Assert.AreEqual(0, view.NumIssues);
            }
        }

        [Test]
        public void UI_ViewManager_IsValid_ReturnsFalseWhenNoViews()
        {
            var emptyViewManager = new ViewManager(Array.Empty<IssueCategory>());
            Assert.IsFalse(emptyViewManager.IsValid());
        }
    }
}
