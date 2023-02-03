using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor.InstructionAnalyzers;

namespace Unity.ProjectAuditor.EditorTests
{
    class DebugLogTests : TestFixtureBase
    {
        TempAsset m_TempAssetClassWithConditionalAttribute;
        TempAsset m_TempAssetClassWithOutConditionalAttribute;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TempAssetClassWithConditionalAttribute = new TempAsset("ClassLoggingWithConditionalAttribute.cs", @"
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

            m_TempAssetClassWithOutConditionalAttribute = new TempAsset("ClassLoggingWithoutConditionalAttribute.cs", @"
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
        }

        [Test]
        public void CodeAnalysis_LoggingMethodWithConditionalAttribute_IsNotReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TempAssetClassWithConditionalAttribute);

            Assert.IsFalse(issues.Any(i => i.descriptor.id == DebugLogAnalyzer.PAC0192));
            Assert.IsFalse(issues.Any(i => i.descriptor.id == DebugLogAnalyzer.PAC0193));
        }

        [Test]
        public void CodeAnalysis_LoggingMethodWithoutConditionalAttribute_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_TempAssetClassWithOutConditionalAttribute);

            Assert.IsTrue(issues.Any(i => i.descriptor.id == DebugLogAnalyzer.PAC0192));
            Assert.IsTrue(issues.Any(i => i.descriptor.id == DebugLogAnalyzer.PAC0193));
        }
    }
}
