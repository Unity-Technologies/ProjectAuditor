<a name="UsingProjectAuditor"></a>
# Extending Project Auditor
Project Auditor has been designed to be modular and this guide provides a brief overview on how to extend Project Auditor.

## Modules
A *module* is a self-contained domain-specific analyzer which reports a list of *issues*.

This is a list of steps to create a module:
1. Create a new module class that inherits from [ProjectAuditorModule](../Editor/ProjectAuditorModule.cs).
2. Define a set of layouts. A layout is used to define name, type and format of the properties of an issue produced by the analysis.
3. Register any module-specific categories. A category is a name used to classify the same kind of issues. 
4. Override the *Audit* method. This is where you will implement your analysis.
5. Override the *GetLayouts* method. This returns a collection of layouts supported by the module.
6. Register a [ViewDescriptor](../Editor/UI/Framework/ViewDescriptor.cs) for the module. This is used to display the module issues in the UI.

Here is an example of a custom module:
```
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEditor;

namespace MyNamespace
{
    class MyModule : ProjectAuditorModule
    {
        static readonly IssueLayout k_IssueLayout = new IssueLayout
        {
            category = ProjectAuditor.GetOrRegisterCategory("New Category"),
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Issue", longName = "Issue description"},
                new PropertyDefinition { type = PropertyType.Area, name = "Area", longName = "The area the issue might have an impact on"}
            }
        };

        public override IEnumerable<IssueLayout> GetLayouts()
        {
            yield return k_IssueLayout;
        }

        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            // Implement your analysis here

            // Create an issue
            projectAuditorParams.onIssueFound(ProjectIssue.Create(k_IssueLayout.category, "This is a test"));

            // Notify the user that the analysis of this module is complete
            projectAuditorParams.onComplete();
        }

        [InitializeOnLoadMethod]
        static void RegisterView()
        {
            ViewDescriptor.Register(new ViewDescriptor
            {
                category = k_IssueLayout.category,
                name = "New Category",
                menuLabel = "MyModule/New Category",
                showFilters = true
            });
        }
    }
}
```
