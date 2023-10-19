using System;
using System.Collections;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Tests.Common;
using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEngine.Networking;
using UnityEngine.TestTools;

namespace Unity.ProjectAuditor.EditorTests
{
    internal class DocumentationTests : TestFixtureBase
    {
        [UnityTest]
        [Ignore("Known failure. This requires a change to be tagged.")]
        public IEnumerator Documentation_Pages_Exist()
        {
            var viewManager = new ViewManager((IssueCategory[])Enum.GetValues(typeof(IssueCategory)));
            viewManager.Create(new Editor.ProjectAuditor(), new ProjectAuditorParams().Rules, new ViewStates());

            for (var i = 0; i < viewManager.NumViews; i++)
            {
                if (viewManager.GetView(i).Desc.category == IssueCategory.Metadata)
                    continue;
                if (viewManager.GetView(i).Desc.category == IssueCategory.FirstCustomCategory)
                    continue;

                var documentationUrl = viewManager.GetView(i).DocumentationUrl;
                var request = UnityWebRequest.Get(documentationUrl);
                yield return request.SendWebRequest();

                Assert.True(request.isDone);
#if UNITY_2020_1_OR_NEWER
                Assert.AreEqual(UnityWebRequest.Result.Success, request.result, $"Page {documentationUrl} not found.");
#else
                Assert.IsFalse(request.isNetworkError || request.isHttpError, $"Page {documentationUrl} not found.");
#endif
            }
        }
    }
}
