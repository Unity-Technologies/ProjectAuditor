using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.ProjectAuditor.EditorTests
{
    class ProblemDescriptorTests
    {
        [Test]
        public void ProblemDescriptor_Comparison_Works()
        {
            var a = new Descriptor
                (
                "TD2001",
                "test",
                Area.CPU,
                "this is not actually a problem",
                "do nothing"
                );
            var b = new Descriptor
                (
                "TD2001",
                "test",
                Area.CPU,
                "this is not actually a problem",
                "do nothing"
                );

            Assert.True(a.Equals(a));
            Assert.True(a.Equals((object)a));
            Assert.True(a.Equals(b));
            Assert.True(a.Equals((object)b));
            b = null;
            Assert.False(a.Equals(b));
            Assert.False(a.Equals((object)b));
        }

        [Test]
        public void ProblemDescriptor_Hash_IsId()
        {
            var p = new Descriptor
                (
                "TD2001",
                "test",
                Area.CPU,
                "this is not actually a problem",
                "do nothing"
                );

            Assert.AreEqual(p.id.GetHashCode(), p.GetHashCode());
        }

        [Test]
        public void ProblemDescriptor_Version_IsCompatible()
        {
            var desc = new Descriptor
                (
                "TD2001",
                "test",
                Area.CPU,
                "this is not actually a problem",
                "do nothing"
                );

            // check default values
            Assert.True(DescriptorLoader.IsVersionCompatible(desc));

            desc.minimumVersion = string.Empty;
            desc.maximumVersion = string.Empty;
            Assert.True(DescriptorLoader.IsVersionCompatible(desc));

            desc.minimumVersion = "0.0";
            desc.maximumVersion = null;
            Assert.True(DescriptorLoader.IsVersionCompatible(desc));

            desc.minimumVersion = null;
            desc.maximumVersion = "0.0";
            Assert.False(DescriptorLoader.IsVersionCompatible(desc));

            desc.minimumVersion = null;
            desc.maximumVersion = "9999.9";
            Assert.True(DescriptorLoader.IsVersionCompatible(desc));

            desc.minimumVersion = "9999.9";
            desc.maximumVersion = null;
            Assert.False(DescriptorLoader.IsVersionCompatible(desc));

            desc.minimumVersion = InternalEditorUtility.GetUnityVersion().ToString();
            desc.maximumVersion = null;
            Assert.True(DescriptorLoader.IsVersionCompatible(desc));

            desc.minimumVersion = null;
            desc.maximumVersion = InternalEditorUtility.GetUnityVersion().ToString();
            Assert.True(DescriptorLoader.IsVersionCompatible(desc));

            desc.minimumVersion = "1.1";
            desc.maximumVersion = "1.0";
            var result = DescriptorLoader.IsVersionCompatible(desc);
            LogAssert.Expect(LogType.Error, "Descriptor (TD2001) minimumVersion (1.1) is greater than maximumVersion (1.0).");
            Assert.False(result);
        }

        [Test]
        public void ProblemDescriptor_MultipleAreas_AreCorrect()
        {
            var desc = new Descriptor
                (
                "TD2001",
                "test",
                new[] {Area.CPU, Area.Memory},
                "this is not actually a problem",
                "do nothing"
                );
            Assert.AreEqual(2, desc.GetAreas().Length);
            Assert.Contains(Area.CPU, desc.GetAreas());
            Assert.Contains(Area.Memory, desc.GetAreas());
        }

        [Test]
        public void ProblemDescriptor_AnyPlatform_IsCompatible()
        {
            var desc = new Descriptor
                (
                "TD2001",
                "test",
                new[] {Area.CPU}
                );

            Assert.True(DescriptorLoader.IsPlatformCompatible(desc));
        }

        [Test]
        public void ProblemDescriptor_Platform_IsCompatible()
        {
            var desc = new Descriptor
                (
                "TD2001",
                "test",
                new[] {Area.CPU}
                )
            {
#if UNITY_EDITOR_WIN
                platforms = new[] { BuildTarget.StandaloneWindows64.ToString() }
#elif UNITY_EDITOR_OSX
                platforms = new[] { BuildTarget.StandaloneOSX.ToString() }
#else
                platforms = new[] { BuildTarget.StandaloneLinux64.ToString() }
#endif
            };

            Assert.True(DescriptorLoader.IsPlatformCompatible(desc));
        }

        [Test]
        public void ProblemDescriptor_Platform_IsNotCompatible()
        {
            var desc = new Descriptor
                (
                "TD2001",
                "test",
                new[] {Area.CPU}
                )
            {
                platforms = new[] { BuildTarget.Android.ToString() }  // assuming Android is not installed by default
            };

            Assert.False(DescriptorLoader.IsPlatformCompatible(desc));
        }

        [Test]
        [TestCase("ApiDatabase")]
        [TestCase("ProjectSettings")]
        public void ProblemDescriptor_Descriptors_AreCorrect(string jsonFilename)
        {
            var regExp = new Regex("^[a-z]{3}[0-9]{4}", RegexOptions.IgnoreCase);
            var descriptors = DescriptorLoader.LoadFromJson(Editor.ProjectAuditor.DataPath, jsonFilename);
            foreach (var descriptor in descriptors)
            {
                Assert.NotNull(descriptor.id);
                Assert.True(regExp.IsMatch(descriptor.id), "Descriptor id format is not valid: " + descriptor.id);
                Assert.NotNull(descriptor.areas);
            }
        }

        [Test]
        [TestCase("ApiDatabase")]
        [TestCase("ProjectSettings")]
        public void ProblemDescriptor_TypeAndMethods_Exist(string jsonFilename)
        {
            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).ToArray();
            var skippableMethodNames = new[]
            {
                "*",
                "OnGUI",
                "OnTriggerStay",
                "OnTriggerStay2D",
                "OnCollisionStay"
            };

            var descriptors = DescriptorLoader.LoadFromJson(Editor.ProjectAuditor.DataPath, jsonFilename);
            foreach (var desc in descriptors)
            {
                Assert.False(string.IsNullOrEmpty(desc.id));
                Assert.False(string.IsNullOrEmpty(desc.description), desc.id + " has no problem description");
                Assert.False(string.IsNullOrEmpty(desc.solution), desc.id + " has no solution description");

                var type = types.FirstOrDefault(t => t.FullName.Equals(desc.type));

                Assert.True(!string.IsNullOrEmpty(desc.customevaluator) || (desc.method.Equals("*") || type != null), "Invalid Type : " + desc.type);

                if (skippableMethodNames.Contains(desc.method))
                    continue;

                try
                {
                    Assert.True(!string.IsNullOrEmpty(desc.customevaluator) || type.GetMethod(desc.method) != null || type.GetProperty(desc.method) != null, "{0} does not belong to {1}", desc.method, desc.type);
                }
                catch (AmbiguousMatchException)
                {
                    // as long as there is a match, this is fine
                }
            }
        }

#if UNITY_2019_1_OR_NEWER
        [Test]
        [TestCase("ApiDatabase")]
        [TestCase("ProjectSettings")]
        public void ProblemDescriptor_Areas_Exist(string jsonFilename)
        {
            var descriptors = Json.FromFile<Descriptor>(PathUtils.Combine(Editor.ProjectAuditor.DataPath, jsonFilename) + ".json");
            foreach (var desc in descriptors)
            {
                for (int i = 0; i < desc.areas.Length; i++)
                {
                    Area area;
                    Assert.True(Enum.TryParse(desc.areas[i], out area), "Invalid area {0} for descriptor {1}", desc.areas[i], desc.id);
                }
            }
        }

#endif

        [Test]
        public void ProblemDescriptor_Platform_IsCorrect()
        {
            var descriptors = Json.FromFile<Descriptor>(PathUtils.Combine(Editor.ProjectAuditor.DataPath, "ProjectSettings") + ".json");
            var platDescriptor = descriptors.FirstOrDefault(d => d.id.Equals("PAS0004"));
            Assert.NotNull(platDescriptor);
            Assert.NotNull(platDescriptor.platforms);
            Assert.Contains("Android", platDescriptor.platforms);
        }
    }
}
