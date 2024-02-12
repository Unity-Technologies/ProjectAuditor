<a name="BuildSteps"></a>
# Build Steps View
The Build Steps View shows all the build steps from the last clean
[BuildReport](https://docs.unity3d.com/ScriptReference/Build.Reporting.BuildReport.html) and how long each step took.

A clean build is important for capturing accurate information about build times and steps. For this reason, Project
Auditor does not display the results of incremental builds.

To create a clean build, follow these steps:
* Open the **File > Build Settings...** window.
* Next to the **Build button**, select the drop-down.
* Select **Clean Build**.

If your project uses a custom build script, ensure that it passes the **BuildOptions.CleanBuildCache** option to
**BuildPipeline.BuildPlayer**.

<img src="images/build-steps.png">

The **Information** panel contains details of the Build Report which the asset file data was extracted from.

<!--- TODO - if you upload a new image, make sure that the times in this paragraph match the times in the image -->
The table differs slightly from the ones in other Views. It represents an ordered hierarchical list of build steps,
indented to show sub-steps. For example, in the image above `Collecting assembly reference` is a sub-task of
`Postprocess built player`, and the 41 seconds of post-processing time includes the 7 seconds spent collecting assembly
references. As a result of this table formatting, the table cannot be sorted by column, and several buttons in the table
view controls are not displayed in this View.

| Column Name | Column Description                                                                                                                                           | 
|-------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Log Level**   | The log level of the build message (Error/Warning/Info).                                                                                                     |
| **Build Step**  | The build report log message. Info messages describe the build step itself, Warning/Error messages indicate problems that were encountered during the build. |
| **Duration**    | The time taken by this build step and its sub-tasks.                                                                                                         |


