using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using UnityEngine;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    class RuleTest
    {
        [Test]
        public void RuleTestPass()
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();
            var settingsAuditor = projectAuditor.GetAuditor<SettingsAuditor>();
            var descriptors = settingsAuditor.GetDescriptors();
            var config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();

            var firstDescriptor = descriptors.FirstOrDefault();
            Rule rule = config.GetRule(firstDescriptor);
            Assert.IsNull(rule);
            
            config.AddRule(new Rule
            {
                id = firstDescriptor.id,
                action = Rule.Action.None
            });
            
            rule = config.GetRule(firstDescriptor);
            Assert.IsNotNull(rule);            

            rule = config.GetRule(firstDescriptor, "some filter");
            Assert.IsNull(rule);            

        }
    }
}