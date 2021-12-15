<a name="UsingProjectAuditor"></a>
# Getting Started with Project Auditor
This guide provides a brief overview on how to use Project Auditor.

## User Interface
To open the Project Auditor window in Unity, go to Window => Analysis => Project Auditor.

<img src="images/window-menu.png">

Once the Project Auditor window is opened. Press *Analyze* to analyse the project.

<img src="images/intro.png">

The analysis might take several seconds, depending on how large the project is. Once the analysis completes, Project Auditor will show a summary of the report.

<img src="images/summary.png">

From the summary it's possible to open one of the diagnostics views (Code or Settings).

<img src="images/overview.png">

The filters allow the user to search through the list of potential issues by string, Assembly and other criterias.

<img src="images/filters.png">

The issues are displayed in a table containing details regarding the impacted area and other properties that depend on the type of issue (such as filename, assembly, etc.)

<img src="images/issues.png">

The panels on the right hand side of the window provide additional information regarding the selected issue. The top panel shows an extended description of the problem, the next panel down contains a recommendation on how to solve the problem, and (when viewing script issues) the bottom panel shows an inverted call tree which allows you to see all of the code paths which lead to the currently-selected line of code.

<img src="images/panels.png">

The mute/unmute buttons can be used to silence specific issues, or groups of issues, that are currently selected.

<img src="images/mute.png">

## Build Reports
A BuildReport contains information about a specific build, and helps you profile the time spent building your project and the builds disk size footprint. This information may help you improving your build times and build sizes.

After each build the [BuildReport](https://docs.unity3d.com/ScriptReference/Build.Reporting.BuildReport.html) is saved for later inspection. However, this is not normally accessible or exposed to the user. Project Auditor allows you to view BuildReports in two different ways:
* By analyzing the project, the last BuildReport will be automatically included in the report.
* By selecting a BuildReport asset, Build Steps and Size information will be shown in the inspector window.

Note that, by default BuildReport assets are not visible to the user. This is to avoid situations in which a new build report is added to the project after every build, which could make version control more complicated or bloat projects over time. In order to automatically create an accessible BuildReport asset for every build, enable the *Save Build Reports* option in the *ProjectAuditorConfig* asset which typically is in *Assets/Editor*. If this option is enabled, build reports will be created in *Assets/BuildReports*.

<img src="images/save-build-reports.png">

## Running from command line
Project Auditor is not a standalone application. However, since it is a Unity Editor tool and provides a C# API, its analysis can be executed from command line by launching the editor in batch mode. This requires an editor script that creates a ProjectAuditor instance and runs the analysis, for example:

```
using Unity.ProjectAuditor.Editor;
using UnityEngine;

public static class ProjectAuditorCI
{
    public static void AuditAndExport()
    {
        var assetPath = "Assets/Editor/ProjectAuditorConfig.asset";
        var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor(assetPath);
        var projectReport = projectAuditor.Audit();
        
        var codeIssues = projectReport.GetIssues(IssueCategory.Code);
        
        Debug.Log("Project Auditor found " + codeIssues.Length + " code issues");
    }
}
```
For more information on how to run Unity via command line, please see the [manual](https://docs.unity3d.com/Manual/CommandLineArguments.html).
