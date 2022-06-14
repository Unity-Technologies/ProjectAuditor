using System;
using System.Collections;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using UnityEditorInternal;
using UnityEngine.TestTools;

namespace Unity.ProjectAuditor.EditorTests
{
    class ProjectAuditorTests
    {
        [Test]
        public void ProjectAuditor_IsInstantiated()
        {
            Activator.CreateInstance(typeof(Unity.ProjectAuditor.Editor.ProjectAuditor));
        }

        [Test]
        public void ProjectAuditor_Module_IsSupported()
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();
#if BUILD_REPORT_API_SUPPORT
            Assert.True(projectAuditor.IsModuleSupported(IssueCategory.BuildFile));
#else
            Assert.False(projectAuditor.IsModuleSupported(IssueCategory.BuildFile));
#endif
        }

        [Test]
        public void ProjectAuditor_Category_IsRegistered()
        {
            var testCategoryName = "TestCategory";
            var numCategories = Unity.ProjectAuditor.Editor.ProjectAuditor.NumCategories();
            var category = Unity.ProjectAuditor.Editor.ProjectAuditor.GetOrRegisterCategory(testCategoryName);

            // check category is registered
            Assert.True(category >= IssueCategory.FirstCustomCategory);

            // check num category increased by 1
            Assert.AreEqual(numCategories + 1, Unity.ProjectAuditor.Editor.ProjectAuditor.NumCategories());

            // check category is still the same
            Assert.AreEqual(category, Unity.ProjectAuditor.Editor.ProjectAuditor.GetOrRegisterCategory(testCategoryName));
        }

        [UnityTest]
        public IEnumerator ProjectAuditor_Callbacks_AreCalled()
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();

            var onUpdateIsCalled = false;
            var onCompleteIsCalled = false;
            projectAuditor.AuditAsync(new ProjectAuditorParams
            {
                onAuditAsyncUpdate = issues =>
                {
                    Assert.True(InternalEditorUtility.CurrentThreadIsMainThread(), "onUpdateAsync was not called on the Main thread");
                    onUpdateIsCalled = true;
                },
                onAuditAsyncComplete = report =>
                {
                    Assert.True(InternalEditorUtility.CurrentThreadIsMainThread(), "onComplete was not called on the Main thread");
                    onCompleteIsCalled = true;
                }
            }).Wait();

            // wait a frame before checking callbacks
            yield return null;

            Assert.True(onUpdateIsCalled);
            Assert.True(onCompleteIsCalled);
        }
    }
}
