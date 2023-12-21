using System;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.EditorTests
{
    class LocationTests
    {
        [Test]
        public void Location_NoExtension_IsValid()
        {
            var location = new Location("Resources/unity_builtin_extra");
            Assert.AreEqual(string.Empty, location.Extension);
            Assert.AreEqual("unity_builtin_extra", location.Filename);
            Assert.AreEqual("Resources/unity_builtin_extra", location.Path);
        }

        [Test]
        public void Location_AssetPath_IsValid()
        {
            var location = new Location("some/path/texture.png");
            Assert.IsTrue(location.IsValid);
            Assert.AreEqual("texture.png", location.Filename);
            Assert.AreEqual("texture.png", location.FormattedFilename);
            Assert.AreEqual("some/path/texture.png", location.Path);
            Assert.AreEqual("some/path/texture.png", location.FormattedPath);
            Assert.AreEqual("png", location.Extension);
            Assert.AreEqual(0, location.Line);
        }

        [Test]
        public void Location_SourcePath_IsValid()
        {
            const int lineNumber = 6;
            var location = new Location("some/path/script.cs", lineNumber);
            Assert.IsTrue(location.IsValid);
            Assert.AreEqual("script.cs:6", location.FormattedFilename);
            Assert.AreEqual("some/path/script.cs:6", location.FormattedPath);
            Assert.AreEqual("some/path/script.cs", location.Path);
            Assert.AreEqual("cs", location.Extension);
            Assert.AreEqual(6, location.Line);
        }

        [Test]
        public void Location_SettingPath_IsValid()
        {
            var location = new Location("Project/Player");
            Assert.IsTrue(location.IsValid);
            Assert.AreEqual("Project/Player", location.Path);
        }

        [Test]
        public void Location_UnityPath_IsShortened()
        {
            var filename = "Dummy.cs";
            var path = PathUtils.Combine(EditorApplication.applicationContentsPath, filename);
            var location = new Location(path);

            // check path after initialization
            Assert.AreEqual(path, location.Path);
            Assert.IsTrue(location.PathForJson.StartsWith("UNITY_PATH/Data"));

            location.PathForJson = location.PathForJson;

            // check path after fake serialization
            Assert.AreEqual(path, location.Path);
            Assert.IsTrue(location.PathForJson.StartsWith("UNITY_PATH/Data"));
        }

        [Test]
        public void Location_ProjectPath_IsRemoved()
        {
            var relativePath = "OutsideAssetsFolder/Dummy.cs";
            var path = PathUtils.Combine(ProjectAuditor.Editor.ProjectAuditor.ProjectPath, relativePath);
            var location = new Location(path);

            Assert.AreEqual(relativePath, location.Path);
        }
    }
}
