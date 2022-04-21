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
        TempAsset m_TempAssetError;

        [SetUp]
        public void SetUp()
        {
            m_TempAssetWarning = new TempAsset("ClassWithObsoleteMethodWarning.cs", @"
class ClassWithObsoleteMethodWarning
{
    [System.Obsolete(""Try not to use me"")]
    void LegacyMethod()
    {}
}"
            );

            m_TempAssetError = new TempAsset("ClassWithObsoleteMethodError.cs", @"
class ClassWithObsoleteMethodError
{
    [System.Obsolete(""Do not use me, ever!"", true)]
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
            Assert.AreEqual("Obsolete method", diag.descriptor.description);

            Assert.AreEqual("Method 'LegacyMethod' is obsolete: Try not to use me", diag.description);
            Assert.AreEqual("System.Void ClassWithObsoleteMethodWarning::LegacyMethod()", diag.GetContext());
        }

        [Test]
        public void CodeAnalysis_ObsoleteMethod_ErrorIsReported()
        {
            var diagnostics = Utility.AnalyzeAndFindAssetIssues(m_TempAssetError);

            Assert.AreEqual(1, diagnostics.Length);

            var diag = diagnostics[0];

            Assert.NotNull(diag);
            Assert.NotNull(diag.descriptor);

            Assert.AreEqual(Rule.Severity.Default, diag.descriptor.severity);
            Assert.AreEqual(Rule.Severity.Error, diag.severity);
            Assert.AreEqual("Obsolete method", diag.descriptor.description);

            Assert.AreEqual("Method 'LegacyMethod' is obsolete: Do not use me, ever!", diag.description);
            Assert.AreEqual("System.Void ClassWithObsoleteMethodError::LegacyMethod()", diag.GetContext());
        }
    }
}
