using System.Collections;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEngine.Networking;
using UnityEngine.TestTools;

namespace Unity.ProjectAuditor.EditorTests
{
    public class DocumentationTests
    {
        [UnityTest]
        public IEnumerator Documentation_Page_Exists()
        {
            var viewManager = new ViewManager();

            viewManager.Create(new Editor.ProjectAuditor(), new ViewStates());
            for (var i = 0; i < viewManager.numViews; i++)
            {
                var documentationUrl = viewManager.GetView(i).documentationUrl;
                var request = UnityWebRequest.Get(documentationUrl);
                yield return request.SendWebRequest();

                Assert.True(request.isDone);
                Assert.AreEqual(UnityWebRequest.Result.Success, request.result, $"Page {documentationUrl} not found.");
            }
        }
    }
}
