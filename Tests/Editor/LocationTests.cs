using System;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    class LocationTests
    {
        [Test]
        public void NoExtensionIsValid()
        {
            var location = new Location("some/path/file");
            Assert.True(location.Extension.Equals(String.Empty));
        }

        [Test]
        public void AssetLocationIsValid()
        {
            const int lineNumber = 6;
            var location = new Location("some/path/file.cs", lineNumber);
            Assert.IsTrue(location.IsValid());
            Assert.IsTrue(location.Filename.Equals("file.cs"));
            Assert.IsTrue(location.Path.Equals("some/path/file.cs"));
            Assert.IsTrue(location.Extension.Equals(".cs"));
            Assert.AreEqual(lineNumber, location.Line);
        }

        [Test]
        public void SettingLocationIsValid()
        {
            var location = new Location("Project/Player");
            Assert.IsTrue(location.IsValid());
            Assert.IsTrue(location.Path.Equals("Project/Player"));
        }
    }
}
