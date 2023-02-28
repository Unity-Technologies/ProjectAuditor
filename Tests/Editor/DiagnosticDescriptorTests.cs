using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Diagnostic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;

namespace Unity.ProjectAuditor.EditorTests
{
    class DiagnosticDescriptorTests
    {
        [Test]
        public void DiagnosticDescriptor_Comparison_Works()
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
        public void DiagnosticDescriptor_Hash_IsId()
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
        public void DiagnosticDescriptor_Version_IsCompatible()
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
        public void DiagnosticDescriptor_MultipleAreas_AreCorrect()
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
        public void DiagnosticDescriptor_AnyPlatform_IsCompatible()
        {
            var desc = new Descriptor
                (
                "TD2001",
                "test",
                new[] {Area.CPU},
                "this is not actually a problem",
                "do nothing"
                );

            Assert.True(DescriptorLoader.IsPlatformCompatible(desc));
        }

        [Test]
        public void DiagnosticDescriptor_Platform_IsCompatible()
        {
            var desc = new Descriptor
                (
                "TD2001",
                "test",
                new[] {Area.CPU},
                "this is not actually a problem",
                "do nothing"
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
        public void DiagnosticDescriptor_Platform_IsNotCompatible()
        {
            var desc = new Descriptor
                (
                "TD2001",
                "test",
                new[] {Area.CPU},
                "this is not actually a problem",
                "do nothing"
                )
            {
                platforms = new[] { BuildTarget.Android.ToString() }  // assuming Android is not installed by default
            };

            Assert.False(DescriptorLoader.IsPlatformCompatible(desc));
        }

        [Test]
        public void DiagnosticDescriptor_Descriptor_IsRegistered()
        {
            var desc = new Descriptor
                (
                "TD2001",
                "test",
                new[] { Area.CPU },
                "this is not actually a problem",
                "do nothing"
                );

            var projectAuditor = new Editor.ProjectAuditor();

            projectAuditor.GetModules(IssueCategory.Code)[0].RegisterDescriptor(desc);

            var descriptors = projectAuditor.GetDescriptors().ToList();

            Assert.Contains(desc, descriptors, "Descriptor {0} is not registered", desc.id);
        }

        [Test]
        public void DiagnosticDescriptor_Descriptors_AreValid()
        {
            var regExp = new Regex("^[a-z]{3}[0-9]{4}", RegexOptions.IgnoreCase);

            var projectAuditor = new Editor.ProjectAuditor();
            var descriptors = projectAuditor.GetDescriptors();
            foreach (var descriptor in descriptors)
            {
                Assert.IsFalse(string.IsNullOrEmpty(descriptor.id), "Descriptor has no id (title: {0})", descriptor.title);
                Assert.IsTrue(regExp.IsMatch(descriptor.id), "Descriptor id format is not valid: " + descriptor.id);
                Assert.IsFalse(string.IsNullOrEmpty(descriptor.title), "Descriptor {0} has no title", descriptor.id);
                Assert.IsFalse(string.IsNullOrEmpty(descriptor.description), "Descriptor {0} has no description", descriptor.id);
                Assert.IsFalse(string.IsNullOrEmpty(descriptor.solution), "Descriptor {0} has no solution", descriptor.id);
                Assert.NotNull(descriptor.areas);
            }
        }

        [Test]
        [TestCase("ApiDatabase")]
        [TestCase("ProjectSettings")]
        public void DiagnosticDescriptor_Descriptors_AreRegistered(string jsonFilename)
        {
            var projectAuditor = new Editor.ProjectAuditor();
            var descriptors = projectAuditor.GetDescriptors();

            var loadedDescriptors = DescriptorLoader.LoadFromJson(Editor.ProjectAuditor.DataPath, jsonFilename);
            foreach (var loadedDescriptor in loadedDescriptors)
            {
                Assert.Contains(loadedDescriptor, descriptors, "Descriptor {0} is not registered", loadedDescriptor.id);
            }
        }

        [Test]
        [TestCase("ApiDatabase")]
        [TestCase("ProjectSettings")]
        public void DiagnosticDescriptor_TypeAndMethods_Exist(string jsonFilename)
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
                var type = types.FirstOrDefault(t => t.FullName.Equals(desc.type));

                Assert.True((desc.method.Equals("*") || type != null), "Invalid Type : " + desc.type);

                if (skippableMethodNames.Contains(desc.method))
                    continue;

                try
                {
                    Assert.True(type.GetMethod(desc.method) != null || type.GetProperty(desc.method) != null, "{0} does not belong to {1}", desc.method, desc.type);
                }
                catch (AmbiguousMatchException)
                {
                    // as long as there is a match, this is fine
                }
            }
        }

#if UNITY_2019_1_OR_NEWER
        [Test]
        public void DiagnosticDescriptor_Areas_Exist()
        {
            var projectAuditor = new Editor.ProjectAuditor();
            var descriptors = projectAuditor.GetDescriptors();
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

        [UnityTest]
        public IEnumerator DiagnosticDescriptor_DocumentationUrl_Exist()
        {
            var projectAuditor = new Editor.ProjectAuditor();
            var descriptors = projectAuditor.GetDescriptors();
            foreach (var desc in descriptors)
            {
                if (string.IsNullOrEmpty(desc.documentationUrl))
                    continue;

                var documentationUrl = desc.documentationUrl;
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

        [Test]
        public void DiagnosticDescriptor_UnsupportedPlatform_IsNotLoaded()
        {
            var descriptors = DescriptorLoader.LoadFromJson(Editor.ProjectAuditor.DataPath, "ProjectSettings");
            var platDescriptor = descriptors.FirstOrDefault(d => d.id.Equals("PAS0000"));

            // PAS0000 should only be available if iOS is supported
            Assert.IsNull(platDescriptor);
        }
    }
}
