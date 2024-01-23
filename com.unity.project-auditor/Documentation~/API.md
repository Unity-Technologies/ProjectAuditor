<a name="API"></a>
# Scripting API Overview
Project Auditor is not a standalone application. However, since it is a Unity Editor tool and provides a C# API, its
analysis can be executed from command line by launching the Editor in batch mode. This requires an Editor script that
creates a `ProjectAuditor` instance and runs the analysis. Here is a simple example of such a script.

```
using Unity.ProjectAuditor.Editor;
using UnityEngine;

public static class ProjectAuditorCI
{
    public static void AuditAndExport()
    {
        string reportPath = "C:/Dev/MyProject/project-auditor-report.json"
        var projectAuditor = new ProjectAuditor();
        var report = projectAuditor.Audit();
        report.Save(reportPath);
        
        var codeIssues = report.GetIssues(IssueCategory.Code);
        Debug.Log($"Project Auditor found {codeIssues.Length} code issues");
    }
}
```

This can be useful for performing automated analysis in a CI/CD environment.

For more information on how to run the Unity Editor via command line, please see the
[manual](https://docs.unity3d.com/Manual/EditorCommandLineArguments.html).

The `ProjectAuditor` class provides the interface for running project analysis, via its `Audit()` and `AuditAsync()`
methods, which return a `Report` object. In the code example above, `Audit()` does not take any configuration
parameters, which means it will create and use an `AnalysisParams` object with default values. This results in a full
analysis of the project, targeting the currently-selected build platform and performing a "Player" code build.

To configure analysis differently, or to specify callbacks for some stages in the analysis process, create an
`AnalysisParams` object and configure it as required, then pass it as a parameter into a `ProjectAuditor` Audit method.
For example, the following code performs asynchronous analysis of a project's code (ignoring other areas such as Assets
and Project Settings) on the default player assembly, compiled in debug mode for Android devices. Callbacks are declared
to count and log the number of issues. 

```
int foundIssues = 0;
var analysisParams = new AnalysisParams
{
  Categories = new[] { IssueCategory.Code },
  AssemblyNames = new[] { "Assembly-CSharp" },
  Platform = BuildTarget.Android,
  CodeOptimization = CodeOptimization.Debug,
  OnIncomingIssues = issues => { foundIssues += issues.Count(); },
  OnCompleted = (report) =>
  {
    Debug.Log($"Found {foundIssues} code issues");
    report.Save(reportPath);
  }  
};

projectAuditor.AuditAsync(analysisParams);
```

The `Report` object produced by Project Auditor's analysis can be saved as a JSON file (as demonstrated in the
code examples above), or can be examined via its API. `Report` contains a `SessionInfo` object with information
about the analysis session, including a copy of the `AnalysisParams` which Project Auditor used to configure the
analysis. It also provides several methods to access the report's list of discovered `ReportItem`s. Each
`ReportItem` represents a single Issue or Insight - all the data for a single item in one of the tables that are shown
in the UI Views.

You cab tell the difference between an Issue and an Insight because Issues have a valid `DescriptorId` field and
insights do not. There are a couple of ways to check this:

```
bool isIssue = reportItem.Id.IsValid();

// A slightly more readable alternative...
bool isIssue = reportItem.IsIssue();
```

If you have a valid `DescriptorId`, you can use it to get the corresponding `Descriptor`, which is the object that
described a type of issue - including its details and recommendation strings.

```
var descriptor = reportItem.Id.GetDescriptor();
Debug.Log($"Id: {reportItem.Id.ToString()}");
Debug.Log($"Description: {descriptor.Description}");
Debug.Log($"Recommendation: {descriptor.Recommendation}");
```

Project Auditor also provides an API for creating custom analyzers tailored to the needs of your project. See
[Creating Custom Analyzers](APICustomAnalyzers.md) for further details.
