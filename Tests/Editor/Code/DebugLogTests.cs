using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.InstructionAnalyzers;
using Unity.ProjectAuditor.Editor.Tests.Common;

namespace Unity.ProjectAuditor.EditorTests
{
    class DebugLogTests : TestFixtureBase
    {
        TestAsset m_TestAssetClassWithConditionalAttribute;
        TestAsset m_TestAssetClassWithOutConditionalAttribute;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_TestAssetClassWithConditionalAttribute = new TestAsset("ClassLoggingWithConditionalAttribute.cs", @"
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

class ClassLoggingWithConditionalAttribute
{
    void Caller()
    {
        // this call will be removed by the compiler
        MethodWithConditionalAttribute();
    }

    [Conditional(""ENABLE_LOG_NOT_DEFINED"")]
    void MethodWithConditionalAttribute()
    {
        Debug.Log(""Some Undesired Logging"");
        Debug.LogWarning(""Some Undesired Warning"");
    }
}
");

            m_TestAssetClassWithOutConditionalAttribute = new TestAsset("ClassLoggingWithoutConditionalAttribute.cs", @"
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

class ClassLoggingWithoutConditionalAttribute
{
    void Caller()
    {
        // this call will not be removed by the compiler
        MethodWithoutConditionalAttribute();
    }

    void MethodWithoutConditionalAttribute()
    {
        Debug.Log(""Some Undesired Logging"");
        Debug.LogWarning(""Some Undesired Warning"");
    }
}
");
            AnalyzeTestAssets();
        }

        [Test]
        public void CodeAnalysis_LoggingMethodWithConditionalAttribute_IsNotReported()
        {
            var issues = GetIssuesForAsset(m_TestAssetClassWithConditionalAttribute);

            Assert.IsFalse(issues.Any(i => i.Id == DebugLogAnalyzer.PAC0192));
            Assert.IsFalse(issues.Any(i => i.Id == DebugLogAnalyzer.PAC0193));
        }

        [Test]
        public void CodeAnalysis_LoggingMethodWithoutConditionalAttribute_IsReported()
        {
            var issues = GetIssuesForAsset(m_TestAssetClassWithOutConditionalAttribute);

            Assert.IsTrue(issues.Any(i => i.Id == DebugLogAnalyzer.PAC0192));
            Assert.IsTrue(issues.Any(i => i.Id == DebugLogAnalyzer.PAC0193));
        }
    }
}
