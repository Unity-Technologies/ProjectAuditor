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
    class DocumentationTests : TestFixtureBase
    {
        [UnityTest]
        [Ignore("Known failure. This requires a change to be tagged.")]
        public IEnumerator Documentation_Pages_Exist()
        {
            var viewManager = new ViewManager((IssueCategory[])Enum.GetValues(typeof(IssueCategory)));
            viewManager.Create(new AnalysisParams().Rules, new ViewStates());

            for (var i = 0; i < viewManager.NumViews; i++)
            {
                if (viewManager.GetView(i).Desc.Category == IssueCategory.Metadata)
                    continue;
                if (viewManager.GetView(i).Desc.Category == IssueCategory.FirstCustomCategory)
                    continue;

                var documentationUrl = viewManager.GetView(i).DocumentationUrl;
                var request = UnityWebRequest.Get(documentationUrl);
                yield return request.SendWebRequest();

                Assert.True(request.isDone);
                Assert.AreEqual(UnityWebRequest.Result.Success, request.result, $"Page {documentationUrl} not found.");
            }
        }
    }
}
