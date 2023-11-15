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
using Unity.ProjectAuditor.Editor.Tests.Common;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;

namespace Unity.ProjectAuditor.EditorTests
{
    class DiagnosticDescriptorTests
    {
        readonly Descriptor m_Descriptor = new Descriptor
            (
            "TDD2001",
            "test",
            Area.CPU,
            "this is not actually a problem",
            "do nothing"
            );

        [OneTimeSetUp]
        public void Setup()
        {
            DescriptorLibrary.RegisterDescriptor(m_Descriptor.id, m_Descriptor);
        }

        [Test]
        public void DiagnosticDescriptor_Comparison_Works()
        {
            var a = new Descriptor
                (
                "TDD2001",
                "test",
                Area.CPU,
                "this is not actually a problem",
                "do nothing"
                );
            var b = new Descriptor
                (
                "TDD2001",
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
                "TDD2001",
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
                "TDD2001",
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

            var unityVersionString = Application.unityVersion;
            unityVersionString = unityVersionString.Remove(
                Regex.Match(unityVersionString, "[A-Za-z]").Index);

            desc.minimumVersion = unityVersionString;
            desc.maximumVersion = null;
            Assert.True(desc.IsVersionCompatible());

            desc.minimumVersion = null;
            desc.maximumVersion = unityVersionString;
            Assert.True(desc.IsVersionCompatible());

            desc.minimumVersion = "1.1";
            desc.maximumVersion = "1.0";
            var result = desc.IsVersionCompatible();
            LogAssert.Expect(LogType.Error, "Descriptor (TDD2001) minimumVersion (1.1) is greater than maximumVersion (1.0).");
            Assert.False(result);
        }

        [Test]
        public void DiagnosticDescriptor_MultipleAreas_AreCorrect()
        {
            var desc = new Descriptor
                (
                "TDD2001",
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
                "TDD2001",
                "test",
                new[] {Area.CPU},
                "this is not actually a problem",
                "do nothing"
                );

            Assert.True(desc.IsPlatformSupported());
        }

        [Test]
        public void DiagnosticDescriptor_Platform_IsCompatible()
        {
            var desc = new Descriptor
                (
                "TDD2001",
                "test",
                new[] {Area.CPU},
                "this is not actually a problem",
                "do nothing"
                )
            {
                platforms = new[] { TestFixtureBase.GetStandaloneBuildTarget().ToString() }
            };

            Assert.True(desc.IsPlatformSupported());
        }

        [Test]
        public void DiagnosticDescriptor_Platform_IsNotCompatible()
        {
            var desc = new Descriptor
                (
                "TDD2001",
                "test",
                new[] {Area.CPU},
                "this is not actually a problem",
                "do nothing"
                )
            {
                platforms = new[] { BuildTarget.WSAPlayer.ToString() }  // assuming WSAPlayer is not installed by default
            };

            Assert.False(desc.IsPlatformSupported());
        }

        [Test]
        public void DiagnosticDescriptor_Descriptor_IsRegistered()
        {
            var projectAuditor = new Editor.ProjectAuditor();

            projectAuditor.GetModules(IssueCategory.Code)[0].RegisterDescriptor(m_Descriptor);

            var IDs = projectAuditor.GetDescriptorIDs();

            Assert.NotZero(IDs.Count(id => id.AsString() == m_Descriptor.id), "Descriptor {0} is not registered", m_Descriptor.id);

            // This will throw an exception if the Descriptor is somehow registered in the module but not in the DescriptorLibrary
            var descriptor = new DescriptorID(m_Descriptor.id).GetDescriptor();
        }

        [Test]
        public void DiagnosticDescriptor_Descriptors_AreValid()
        {
            var regExp = new Regex("^[A-Z]{3}\\d{4}$", RegexOptions.IgnoreCase);

            var projectAuditor = new Editor.ProjectAuditor();
            var IDs = projectAuditor.GetDescriptorIDs();
            foreach (var id in IDs)
            {
                var descriptor = id.GetDescriptor();
                Assert.IsFalse(string.IsNullOrEmpty(descriptor.id), "Descriptor has no Id (title: {0})", descriptor.title);
                Assert.IsTrue(regExp.IsMatch(descriptor.id), "Descriptor Id format is not valid: " + descriptor.id);
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
            var IDs = projectAuditor.GetDescriptorIDs();
            foreach (var id in IDs)
            {
                var descriptor = id.GetDescriptor();
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
            var IDs = projectAuditor.GetDescriptorIDs();

            var loadedDescriptors = DescriptorLoader.LoadFromJson(Editor.ProjectAuditor.s_DataPath, jsonFilename);
            foreach (var loadedDescriptor in loadedDescriptors)
            {
                // Only test Descriptors that are compatible with the Unity version and at least one installed build target
                // (We know incompatible Descriptors won't be registered in our instance of ProjectAuditor)
                if (loadedDescriptor.IsPlatformSupported() && loadedDescriptor.IsVersionCompatible())
                {
                    Assert.NotZero(IDs.Count(id => id.AsString() == loadedDescriptor.id),
                        "Descriptor {0} is not registered", loadedDescriptor.id);
                }
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
                // Only test Descriptors that are compatible with the Unity version and at least one installed build target
                // (We know incompatible Descriptors won't be registered in our instance of ProjectAuditor)
                if (!desc.IsPlatformSupported() || !desc.IsVersionCompatible())
                    continue;

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
            var IDs = projectAuditor.GetDescriptorIDs();
            foreach (var id in IDs)
            {
                var descriptor = id.GetDescriptor();
                for (int i = 0; i < descriptor.areas.Length; i++)
                {
                    Area area;
                    Assert.True(Enum.TryParse(descriptor.areas[i], out area), "Invalid area {0} for descriptor {1}", descriptor.areas[i], descriptor.id);
                }
            }
        }

#endif

        [UnityTest]
        public IEnumerator DiagnosticDescriptor_DocumentationUrl_Exist()
        {
            var projectAuditor = new Editor.ProjectAuditor();
            var IDs = projectAuditor.GetDescriptorIDs();
            foreach (var id in IDs)
            {
                var descriptor = id.GetDescriptor();

                if (string.IsNullOrEmpty(descriptor.documentationUrl))
                    continue;

                var documentationUrl = descriptor.documentationUrl;
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
        public void DiagnosticDescriptor_UnsupportedPlatform_IsNotRegistered()
        {
            var projectAuditor = new Editor.ProjectAuditor();
            var IDs = projectAuditor.GetDescriptorIDs();

            // PAS0005 should only be available if iOS Editor component is installed
            var foundID = IDs.Contains(new DescriptorID("PAS0005"));

            // Yamato tests don't have iOS as a supported build target, but we want to pass tests locally as well,
            // where iOS support might be installed.
            var buildTarget = BuildTarget.iOS;
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);

            if (BuildPipeline.IsBuildTargetSupported(buildTargetGroup, buildTarget))
            {
                Assert.IsTrue(foundID);
            }
            else
            {
                Assert.IsFalse(foundID);
            }
        }
    }
}
