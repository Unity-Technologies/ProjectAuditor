<a name="UsingProjectAuditor"></a>
# Getting Started with Project Auditor
This guide provides a brief overview on how to use Project Auditor.

## User Interface
To open the Project Auditor window in Unity, go to Window => Analysis => Project Auditor.

<img src="images/window-menu.png">

Once the Project Auditor window is opened. Press *Analyze* to analyse the project.

<img src="images/intro.png">

The analysis might take several seconds, depending on how large the project is. Once the analysis completes, Project Auditor will show the report of potential issues, filters and additional information.

<img src="images/overview.png">

The issues are classified into several different views. The active view can be selected from the toolbar.

<img src="images/view-selection.png">

The filters allow the user to search through the list of potential issues by string, Assembly and other criterias.

<img src="images/filters.png">

The issues are displayed in a table containing details regarding the impacted area and other properties that depend on the type of issue (such as filename, assembly, etc.)

<img src="images/issues.png">

The panels on the right hand side of the window provide additional information regarding the selected issue. The top panel shows an extended description of the problem, the next panel down contains a recommendation on how to solve the problem, and (when viewing script issues) the bottom panel shows an inverted call tree which allows you to see all of the code paths which lead to the currently-selected line of code.

<img src="images/panels.png">

The mute/unmute buttons can be used to silence specific issues, or groups of issues, that are currently selected.

<img src="images/mute.png">

## Running from command line
Project Auditor analysis can be executed from command line by launching the editor in batch mode. This requires an editor script that:

* Creates a ProjectAuditor object
* Runs the analysis
* Exports the report

Here is an example:

```
using Unity.ProjectAuditor.Editor;
using UnityEngine;

public static class ProjectAuditorCI
{
    public static void AuditAndExport()
    {
        var configFilename = "Assets/Editor/ProjectAuditorConfig.asset";
        var outputFilename = "project-auditor-report.csv";

        var projectAuditor = new ProjectAuditor(configFilename);
        var projectReport = projectAuditor.Audit();
        projectReport.ExportToCSV(outputFilename);
    }
}
```
For more information on how to run Unity via command line, please see the [manual](https://docs.unity3d.com/Manual/CommandLineArguments.html).
