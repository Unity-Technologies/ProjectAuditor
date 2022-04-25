using NUnit.Framework;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.EditorTests
{
    public class PathUtilTests
    {
        [Test]
        public void PathUtils_InvalidChars_AreReplaced()
        {
            Assert.AreEqual("sactx-0-2048x2048-DXT5_BC3-MenusAtlas-e85506f4",
                PathUtils.ReplaceInvalidChars("sactx-0-2048x2048-DXT5|BC3-MenusAtlas-e85506f4"));
        }
    }
}
