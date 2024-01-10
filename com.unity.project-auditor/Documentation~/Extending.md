<a name="UsingProjectAuditor"></a>
# Extending Project Auditor
Project Auditor has been designed to be modular and this guide provides a brief overview on how to extend Project Auditor.

## Modules
A *module* is a self-contained domain-specific analyzer which reports a list of *issues*.

This is a list of steps to create a module:
1. Create a new module class that inherits from [Module](../Editor/Core/Module.cs).
2. Override the *Name* property, which returns a user-friendly module name.
3. Override the *SupportedLayouts* property to return a collection of supported layouts. Note that a layout is used to define name, type and format of the properties of an issue produced by the analysis.
4. If applicable, override the *SupportedDescriptorIds* property to return a collection of supported descriptor IDs. This can be skipped if the module does not report diagnostics.
5. Register any module-specific categories via *ProjectAuditor.GetOrRegisterCategory*. Note that a category is a unique name used to classify the same kind of issues. 
6. Override the *Audit* method. This is where you will implement your analysis.
   1. Create [ProjectIssue](../Editor/API/ProjectIssue.cs) objects, if any
   2. Use the *OnIncomingIssues* to report a batch of issues, if any. This can be used multiple times inside a module.
   3. Return a *AnalysisResult*.
6. Register a [ViewDescriptor](../Editor/UI/Framework/ViewDescriptor.cs) for the module. This is used to display the module issues in the UI.

Here is an example of a custom module:
```
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEditor;

namespace MyNamespace
{
    class MyModule : Module
    {
        static readonly Descriptor k_Descriptor = new Descriptor
        (
            "PAT0000",
            "Test Descriptor",
            Areas.Memory,
            "Explanation of the problem.",
            "Explanation of potential solution."
        );

        static readonly IssueLayout k_IssueLayout = new IssueLayout
        {
            Category = ProjectAuditor.GetOrRegisterCategory("New Category"),
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Name = "Issue", LongName = "Issue description", MaxAutoWidth = 800 },
                new PropertyDefinition { Type = PropertyType.Filename, Name = "File"}
            }
        };

        public override string Name => "My Module";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_IssueLayout
        };

        public override AnalysisResult Audit(AnalysisParams analysisParams, IProgress progress = null)
        {
            var context = new AnalysisContext
            {
                Params = analysisParams
            };

            // Implement your analysis here and issue reporting

            var issues = new List<ProjectIssue>();

            // Create a diagnostic issue
            var diagnostic = context.CreateIssue(k_IssueLayout.Category, k_Descriptor.Id)
                .WithLocation("MyFile.cs", 0);

            issues.Add(diagnostic);

            if (issues.Count > 0)
                analysisParams.OnIncomingIssues(issues);

            return AnalysisResult.Success;
        }

        [InitializeOnLoadMethod]
        static void RegisterView()
        {
            ViewDescriptor.Register(new ViewDescriptor
            {
                Category = k_IssueLayout.Category,
                Name = "New Category",
                MenuLabel = "MyModule/New Category",
                ShowFilters = true
            });
        }
    }
}
```
