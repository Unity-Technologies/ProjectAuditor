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
            
            // make sure there are no rules
            Rule rule = config.GetRule(firstDescriptor);
            Assert.IsNull(rule);

            var filter = "dummy";

            // add rule with a filter.
            config.AddRule(new Rule
            {
                id = firstDescriptor.id,
                action = Rule.Action.None,
                filter = filter 
            });

            // search for non-specific rule for this descriptor
            rule = config.GetRule(firstDescriptor);
            Assert.IsNull(rule);            

            // search for specific rule
            rule = config.GetRule(firstDescriptor, filter);
            Assert.IsNotNull(rule);            
            
            // add rule with no filter, which will replace any specific rule
            config.AddRule(new Rule
            {
                id = firstDescriptor.id,
                action = Rule.Action.None
            });

            // search for specific rule again
            rule = config.GetRule(firstDescriptor, filter);
            Assert.IsNull(rule);            

            // search for non-specific rule again
            rule = config.GetRule(firstDescriptor);
            Assert.IsNotNull(rule);            

            // try to delete specific rule which has been already replaced by non-specific one
            config.ClearRules(firstDescriptor, filter);
            
            // generic rule should still exist
            rule = config.GetRule(firstDescriptor);
            Assert.IsNotNull(rule);
            
            // try to delete non-specific rule
            config.ClearRules(firstDescriptor);
            rule = config.GetRule(firstDescriptor);
            Assert.IsNull(rule);
        }
    }
}