using System.Collections;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEditor;
using UnityEngine.TestTools;

namespace Unity.ProjectAuditor.EditorTests
{
    internal class ViewTests
    {
        [UnityTest]
        public IEnumerator ViewDescriptor_Register_OnlyOnce()
        {
            // need domain reload so that views are re-registered
            EditorUtility.RequestScriptReload();
            yield return new WaitForDomainReload();

            var testCategoryName = "TestCategoryForViewTests";
            var category = Unity.ProjectAuditor.Editor.ProjectAuditor.GetOrRegisterCategory(testCategoryName);
            var viewDesc = new ViewDescriptor
            {
                Category = category
            };
            Assert.True(ViewDescriptor.Register(viewDesc));

            var numCategories = Unity.ProjectAuditor.Editor.ProjectAuditor.NumCategories();

            // second time, Register should fail
            Assert.False(ViewDescriptor.Register(viewDesc));

            // check num categories is still the same
            Assert.AreEqual(numCategories, Unity.ProjectAuditor.Editor.ProjectAuditor.NumCategories());
        }
    }
}
