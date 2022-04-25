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
        TempAsset m_TempAssetObsoleteMethod;
        TempAsset m_TempAssetObsoleteMethodDefaultMessage;

        [SetUp]
        public void SetUp()
        {
            m_TempAssetObsoleteMethod = new TempAsset("ClassWithCallToObsoleteMethod.cs", @"
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

            m_TempAssetObsoleteMethodDefaultMessage = new TempAsset("ClassWithCallToObsoleteMethodWithDefaultMessage.cs", @"
class ClassWithCallToObsoleteMethodWithDefaultMessage
{
    void Caller()
    {
        LegacyMethod();
    }

    [System.Obsolete()]
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
            var diagnostics = Utility.AnalyzeAndFindAssetIssues(m_TempAssetObsoleteMethod);

            Assert.AreEqual(1, diagnostics.Length);

            var diag = diagnostics[0];

            Assert.NotNull(diag);
            Assert.NotNull(diag.descriptor);

            Assert.AreEqual(Rule.Severity.Default, diag.descriptor.severity);
            Assert.AreEqual(Rule.Severity.Warning, diag.severity);
            Assert.AreEqual("Obsolete method call", diag.descriptor.description);
            Assert.AreEqual("This method is marked as obsolete", diag.descriptor.problem);
            Assert.AreEqual("Try not to use me", diag.descriptor.solution);

            Assert.AreEqual("Call to 'LegacyMethod' obsolete method", diag.description);
            Assert.AreEqual("System.Void ClassWithCallToObsoleteMethod::Caller()", diag.GetContext());
            Assert.AreEqual(6, diag.line);
        }

        [Test]
        public void CodeAnalysis_ObsoleteMethodWithDefaultMessage_WarningIsReported()
        {
            var diagnostics = Utility.AnalyzeAndFindAssetIssues(m_TempAssetObsoleteMethodDefaultMessage);

            Assert.AreEqual(1, diagnostics.Length);

            var diag = diagnostics[0];

            Assert.NotNull(diag);
            Assert.NotNull(diag.descriptor);

            Assert.AreEqual(Rule.Severity.Default, diag.descriptor.severity);
            Assert.AreEqual(Rule.Severity.Warning, diag.severity);
            Assert.AreEqual("Obsolete method call", diag.descriptor.description);
            Assert.AreEqual("This method is marked as obsolete", diag.descriptor.problem);
            Assert.AreEqual("Do not call this method if possible", diag.descriptor.solution);

            Assert.AreEqual("Call to 'LegacyMethod' obsolete method", diag.description);
            Assert.AreEqual("System.Void ClassWithCallToObsoleteMethodWithDefaultMessage::Caller()", diag.GetContext());
            Assert.AreEqual(6, diag.line);
        }
    }
}
