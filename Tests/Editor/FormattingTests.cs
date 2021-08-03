using System;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor.Utils;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    class FormattingTests
    {
        [Test]
        [TestCase((ulong)512, "512 B")]
        [TestCase((ulong)1024, "1.00 KB")]
        [TestCase((ulong)1024 * 1024, "1.00 MB")]
        [TestCase((ulong)1024 * 1024 * 1024, "1.00 GB")]
        public void SizeIsFormatted(ulong asNumber, string asString)
        {
            Assert.True(Formatting.FormatSize(asNumber).Equals(asString));
        }

        [Test]
        public void TimeIsFormatted()
        {
            var time = new TimeSpan(10, 24, 30);
            const string formatted = "10:24:30";

            Assert.True(Formatting.FormatTime(time).Equals(formatted));
        }
    }
}
