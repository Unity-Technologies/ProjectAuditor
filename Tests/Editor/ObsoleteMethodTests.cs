using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.EditorTests
{
    class ObsoleteMethodTests
    {
        TempAsset m_TempAssetWarning;

        [SetUp]
        public void SetUp()
        {
            m_TempAssetWarning = new TempAsset("ClassWithCallToObsoleteMethod.cs", @"
class ClassWithCallToObsoleteMethod
{
    void Caller()
    {
        LegacyMethod();
    }

    [System.Obsolete(""Try not to use me"")]
    void LegacyMethod()
    {}
}"
            );
        }

        [TearDown]
        public void TearDown()
        {
            TempAsset.Cleanup();
        }

        [Test]
        public void CodeAnalysis_ObsoleteMethod_WarningIsReported()
        {
            var diagnostics = Utility.AnalyzeAndFindAssetIssues(m_TempAssetWarning);

            Assert.AreEqual(1, diagnostics.Length);

            var diag = diagnostics[0];

            Assert.NotNull(diag);
            Assert.NotNull(diag.descriptor);

            Assert.AreEqual(Rule.Severity.Default, diag.descriptor.severity);
            Assert.AreEqual(Rule.Severity.Warning, diag.severity);
            Assert.AreEqual("Obsolete method call", diag.descriptor.description);

            Assert.AreEqual("Call to 'LegacyMethod' obsolete method: Try not to use me", diag.description);
            Assert.AreEqual("System.Void ClassWithCallToObsoleteMethod::Caller()", diag.GetContext());
            Assert.AreEqual(6, diag.line);
        }
    }
}
