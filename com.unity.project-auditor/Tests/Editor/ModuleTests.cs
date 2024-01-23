using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Modules;
using UnityEditor;

namespace Unity.ProjectAuditor.EditorTests
{
    class CustomAuditor// : IAuditor
    {
        private readonly Descriptor k_Descriptor =
            new Descriptor("TDD0000", "This is a test descriptor", Areas.CPU, "description", "recommendation");

        readonly IssueLayout m_Layout = new IssueLayout
        {
            Category = IssueCategory.Code,
            Properties = new[]
            {
                new PropertyDefinition {Type = PropertyType.Description }
            }
        };

        // public IEnumerable<Descriptor> GetDescriptors()
        // {
        //     yield return m_Descriptor;
        // }

        public IEnumerable<IssueLayout> GetLayouts()
        {
            yield return m_Layout;
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            DescriptorLibrary.RegisterDescriptor(k_Descriptor.Id, k_Descriptor);
        }

        public void Reload(string path)
        {
        }

        public void RegisterDescriptor(Descriptor descriptor)
        {
        }

        public void Audit(Action<ReportItem> onIssueFound, Action onComplete, IProgress progress = null)
        {
            onIssueFound(new ReportItem(IssueCategory.Code, k_Descriptor.Id, "This is a test issue"));
            onComplete();
        }
    }


    class ModuleTests
    {
        [Test]
        public void Module_BuiltinTypes_Exist()
        {
            var types = TypeCache.GetTypesDerivedFrom(typeof(Module));

            Assert.NotNull(types.FirstOrDefault(type => type == typeof(CodeModule)));
            Assert.NotNull(types.FirstOrDefault(type => type == typeof(SettingsModule)));
        }

        [Test]
        [Ignore("Work in progress")]
        public void Module_Custom_IsCreated()
        {
            //var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();
            //Assert.NotNull(projectAuditor.GetAuditor<CustomAuditor>());
        }

        [Test]
        [Ignore("Work in progress")]
        public void Module_CustomIssue_IsReported()
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();
            var report = projectAuditor.Audit();
            var issues = report.FindByCategory(IssueCategory.Code);
            Assert.NotNull(issues.FirstOrDefault(i => i.Description.Equals("This is a test issue")));
        }
    }
}
