# About Project Auditor
Project Auditor is an experimental static analysis tool that analyzes assets, settings, and scripts of the Unity project and produces a report that contains the following:

* Code and Settings Diagnostics: a list of possible problems that might affect performance, memory and other areas.
* BuildReport: timing and size information of the last build.
* Assets information

## Requirements
Project Auditor is meant to be compatible with Unity versions from 2018 to the latest [Long-Term Support](https://unity3d.com/unity/qa/lts-releases) (recommended). 

Note that most recent Project Auditor version to support 2017 or earlier is [0.5.0-preview](https://github.com/Unity-Technologies/ProjectAuditor/releases/tag/0.5.0-preview).

## Installation
At this time, Project Auditor is not discoverable via Package Manager so it has to be installed manually.

It is recommended to install Project Auditor via the _Add package from git URL_ in Package Manager. For more information on this and alternative installation methods please refer to [Installing Project Auditor](Installing.md).

## How to Use
The Project Auditor editor window can be opened via *Window => Analysis => Project Auditor*. Click the Analyze button, then select a _View_ from the drop-down menu to review the list of potential issues to determine whether they are actual problems in your project. Every View provides:

* A series of filters to narrow down the visible list of issues
* The ability to "Mute" issues which have been investigated and found not to be a problem
* The ability to export the View to a .csv file for use in build reports or automated testing

For more information, check the [Getting started](GettingStarted.md) guide.

For information on a specific view, check the corresponding page:
* [Code](Code.md) (Diagnostics)
* [Settings](Settings.md) (Diagnostics)
* [Assemblies](Assemblies.md)
* [Generics](Generics.md)
* [Resources](Resources.md)
* [Shaders](Shaders.md)
* [Shader Variants](Variants.md)
* [Build Steps](BuildSteps.md) (Build Report - Requires Unity 2019.4 or newer)
* [Build Size](BuildSize.md) (Build Report - Requires Unity 2019.4 or newer)

## Reporting issues
If you have issues running Project Auditor in your Unity project, please report them on the [GitHub repository](https://github.com/Unity-Technologies/ProjectAuditor/issues).

## Package contents
The following table indicates the package directory structure:

|Location|Description|
|---|---|
|`Data`|Contains the issue definition database.|
|`Documentation~`|Contains documentation files.|
|`Editor`|Contains all editor scripts: Project Auditor and external DLLs.|
|`Editor/UI`|Project Auditor Editor window.|
|`Tests`|Contains all scripts required to test the package.|

## Document revision history
|Date|Reason|
|---|---|
|Jul 23, 2021|Added view-specific pages|
|Apr 9, 2021|Updated index page with more detail|
|Feb 15, 2021|Updated documentation|
|Oct 16, 2020|Added information about command line execution|
|May 21, 2020 |Expanded *Using Project Auditor* section|
|Dec 4, 2019|First draft.|
