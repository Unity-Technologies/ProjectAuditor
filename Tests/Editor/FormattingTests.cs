using System;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.EditorTests
{
    class FormattingTests
    {
        [Test]
        [TestCase((ulong)512, "512 B")]
        [TestCase((ulong)1024, "1.00 KB")]
        [TestCase((ulong)1024 * 1024, "1.00 MB")]
        [TestCase((ulong)1024 * 1024 * 1024, "1.00 GB")]
        public void Formatting_Size_IsFormatted(ulong asNumber, string asString)
        {
            Assert.AreEqual(asString, Formatting.FormatSize(asNumber));
        }

        [Test]
        public void Formatting_Time_IsFormatted()
        {
            var time = new TimeSpan(10, 24, 30);
            const string formatted = "10:24:30";

            Assert.AreEqual(formatted, Formatting.FormatTime(time));
        }
    }
}
