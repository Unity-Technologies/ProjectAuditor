using System;
using System.IO;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public class LocationTests
    {
        [Test]
        public void AssetLocationIsValid()
        {
            var location = new Location("some/path/file.cs", 0);
            Assert.IsTrue(location.IsValid());
            Assert.IsTrue(location.Filename.Equals("file.cs"));
            Assert.IsTrue(location.Path.Equals("some/path/file.cs"));
            Assert.IsTrue(location.Type == LocationType.Asset);
        }

        [Test]
        public void SettingLocationIsValid()
        {
            var location = new Location("Project/Player");
            Assert.IsTrue(location.IsValid());
            Assert.IsTrue(location.Path.Equals("Project/Player"));
            Assert.IsTrue(location.Type == LocationType.Setting);
        }
    }
}
