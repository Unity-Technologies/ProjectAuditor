using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;

namespace Unity.ProjectAuditor.EditorTests
{
    class CustomAuditor// : IAuditor
    {
        readonly ProblemDescriptor m_Descriptor = new ProblemDescriptor(666, "This is a test descriptor", Area.CPU);

        private readonly IssueLayout m_Layout = new IssueLayout
        {
            category = IssueCategory.Code,
            properties = new[]
            {
                new PropertyDefinition {type = PropertyType.Description }
            }
        };

        public IEnumerable<ProblemDescriptor> GetDescriptors()
        {
            yield return m_Descriptor;
        }

        public IEnumerable<IssueLayout> GetLayouts()
        {
            yield return m_Layout;
        }

        public void Initialize(ProjectAuditorConfig config)
        {
        }

        public void Reload(string path)
        {
        }

        public void RegisterDescriptor(ProblemDescriptor descriptor)
        {
        }

        public void Audit(Action<ProjectIssue> onIssueFound, Action onComplete, IProgress progress = null)
        {
            onIssueFound(new ProjectIssue(m_Descriptor, "This is a test issue", IssueCategory.Code));
            onComplete();
        }
    }


    class ModuleTests
    {
        [Test]
        public void Module_BuiltinTypes_Exist()
        {
            var types = TypeCache.GetTypesDerivedFrom(typeof(ProjectAuditorModule));

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
            var issues = report.GetIssues(IssueCategory.Code);
            Assert.NotNull(issues.FirstOrDefault(i => i.description.Equals("This is a test issue")));
        }
    }
}
