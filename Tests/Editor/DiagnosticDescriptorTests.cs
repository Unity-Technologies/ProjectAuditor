using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Core;
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
            Assert.True(desc.IsVersionCompatible());

            desc.minimumVersion = string.Empty;
            desc.maximumVersion = string.Empty;
            Assert.True(desc.IsVersionCompatible());

            desc.minimumVersion = "0.0";
            desc.maximumVersion = null;
            Assert.True(desc.IsVersionCompatible());

            desc.minimumVersion = null;
            desc.maximumVersion = "0.0";
            Assert.False(desc.IsVersionCompatible());

            desc.minimumVersion = null;
            desc.maximumVersion = "9999.9";
            Assert.True(desc.IsVersionCompatible());

            desc.minimumVersion = "9999.9";
            desc.maximumVersion = null;
            Assert.False(desc.IsVersionCompatible());

            desc.minimumVersion = InternalEditorUtility.GetUnityVersion().ToString();
            desc.maximumVersion = null;
            Assert.True(desc.IsVersionCompatible());

            desc.minimumVersion = null;
            desc.maximumVersion = InternalEditorUtility.GetUnityVersion().ToString();
            Assert.True(desc.IsVersionCompatible());

            desc.minimumVersion = "1.1";
            desc.maximumVersion = "1.0";
            var result = desc.IsVersionCompatible();
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

            Assert.True(desc.IsPlatformCompatible());
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

            Assert.True(desc.IsPlatformCompatible());
        }

#if !UNITY_2022_2_OR_NEWER
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
                platforms = new[] { BuildTarget.Lumin.ToString() }  // assuming Stadia is not installed by default
            };

            Assert.False(desc.IsPlatformCompatible());
        }
#endif

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

            var IDs = projectAuditor.GetIDs();

            Assert.Contains(desc.id, IDs, "Descriptor {0} is not registered", desc.id);
            Assert.IsTrue(DescriptorLibrary.TryGetDescriptor(desc.id, out var descriptor), "Descriptor {0} not found in DescriptorLibrary", desc.id);
        }

        [Test]
        public void DiagnosticDescriptor_Descriptors_AreValid()
        {
            var regExp = new Regex("^[A-Z]{3}\\d{4}$", RegexOptions.IgnoreCase);

            var projectAuditor = new Editor.ProjectAuditor();
            var IDs = projectAuditor.GetIDs();
            foreach (var id in IDs)
            {
                Assert.IsTrue(DescriptorLibrary.TryGetDescriptor(id, out var descriptor), "Descriptor {0} not found in DescriptorLibrary", id);
                Assert.IsFalse(string.IsNullOrEmpty(descriptor.id), "Descriptor has no id (title: {0})", descriptor.title);
                Assert.IsTrue(regExp.IsMatch(descriptor.id), "Descriptor id format is not valid: " + descriptor.id);
                Assert.IsFalse(string.IsNullOrEmpty(descriptor.title), "Descriptor {0} has no title", descriptor.id);
                Assert.IsFalse(string.IsNullOrEmpty(descriptor.description), "Descriptor {0} has no description", descriptor.id);
                Assert.IsFalse(string.IsNullOrEmpty(descriptor.solution), "Descriptor {0} has no solution", descriptor.id);

                Assert.NotNull(descriptor.areas);
            }
        }

        [Test]
        public void DiagnosticDescriptor_Descriptors_AreFormatted()
        {
            var projectAuditor = new Editor.ProjectAuditor();
            var IDs = projectAuditor.GetIDs();
            foreach (var id in IDs)
            {
                Assert.IsTrue(DescriptorLibrary.TryGetDescriptor(id, out var descriptor), "Descriptor {0} not found in DescriptorLibrary", id);
                Assert.IsFalse(descriptor.title.EndsWith("."), "Descriptor {0} string must not end with a full stop. String: {1}", descriptor.id, descriptor.title);
                Assert.IsTrue(descriptor.description.EndsWith("."), "Descriptor {0} string must end with a full stop. String: {1}", descriptor.id, descriptor.description);
                Assert.IsTrue(descriptor.solution.EndsWith("."), "Descriptor {0} string must end with a full stop. String: {1}", descriptor.id, descriptor.solution);
                Assert.IsFalse(descriptor.messageFormat.EndsWith("."), "Descriptor {0} string must not end with a full stop. String: {1}", descriptor.id, descriptor.messageFormat);

                CheckHtmlTags(descriptor.description);
                CheckHtmlTags(descriptor.solution);

                Assert.NotNull(descriptor.areas);
            }
        }

        void CheckHtmlTags(string input)
        {
            // Regular expression pattern for matching HTML tags
            var pattern = @"<[^>]+?>";

            // Match the pattern against the input string
            var matches = Regex.Matches(input, pattern);

            // Stack to track opening tags
            var tagStack = new Stack<string>();

            // Iterate through each matched tag
            foreach (Match match in matches)
            {
                CheckTag(input, match.Value, tagStack);
            }

            // Check if all opening tags have matching closing tags
            Assert.AreEqual(0, tagStack.Count, "String: {0}", input);
        }

        void CheckTag(string text, string tag, Stack<string> tagStack)
        {
            if (tag.Equals("<T>", StringComparison.OrdinalIgnoreCase))
                return;

            var pattern = @"^<([a-zA-Z]+)>$|^<\/([a-zA-Z]+)>$";

            Assert.IsTrue(Regex.IsMatch(tag, pattern), tag);

            if (tag.Equals("<b>", StringComparison.OrdinalIgnoreCase))
            {
                Assert.AreEqual(0, tagStack.Count, "Nested tags are not allowed. String: {0}", text);

                // Opening <b> tag
                tagStack.Push(tag);
            }
            else if (tag.Equals("</b>", StringComparison.OrdinalIgnoreCase))
            {
                // Closing </b> tag
                if (tagStack.Count == 0 || !tagStack.Pop().Equals("<b>", StringComparison.OrdinalIgnoreCase))
                {
                    Assert.Fail("No corresponding opening <b> tag found. Tag: {0}. String {1}", tag, text);
                }
            }
            else
            {
                Assert.IsFalse(tag.EndsWith("/>"), "Self-closing tags are not allowed. Tag: {0}. String {1}", tag, text);
                Assert.Fail("Invalid/Unsupported Tag: {0}. String {1}", tag, text);
            }
        }

        [Test]
        [TestCase("ApiDatabase")]
        [TestCase("ProjectSettings")]
        public void DiagnosticDescriptor_Descriptors_AreRegistered(string jsonFilename)
        {
            var projectAuditor = new Editor.ProjectAuditor();
            var IDs = projectAuditor.GetIDs();

            var loadedDescriptors = DescriptorLoader.LoadFromJson(Editor.ProjectAuditor.s_DataPath, jsonFilename);
            foreach (var loadedDescriptor in loadedDescriptors)
            {
                Assert.Contains(loadedDescriptor.id, IDs, "Descriptor {0} is not registered", loadedDescriptor.id);
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

            var descriptors = DescriptorLoader.LoadFromJson(Editor.ProjectAuditor.s_DataPath, jsonFilename);
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
            var IDs = projectAuditor.GetIDs();
            foreach (var id in IDs)
            {
                Assert.IsTrue(DescriptorLibrary.TryGetDescriptor(id, out var desc), "Descriptor {0} not found in DescriptorLibrary", id);
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
            var IDs = projectAuditor.GetIDs();
            foreach (var id in IDs)
            {
                Assert.IsTrue(DescriptorLibrary.TryGetDescriptor(id, out var desc), "Descriptor {0} not found in DescriptorLibrary", id);

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
            var descriptors = DescriptorLoader.LoadFromJson(Editor.ProjectAuditor.s_DataPath, "ProjectSettings");
            var platDescriptor = descriptors.FirstOrDefault(d => d.id.Equals("PAS0010"));

            // PAS0010 should only be available if WebGL is supported
            Assert.IsNull(platDescriptor);
        }
    }
}
