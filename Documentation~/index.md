# About Project Auditor
Project Auditor is a static analysis tool for Unity projects, which finds potential issues related to memory consumption, performance and other areas.

Use the Project Auditor package to analyse scripts and settings of your project. The tool creates a list of potential issues which have been identified, then the user will need to determine whether they are real issues or not.

## Installation
At this time, Project Auditor is not discoverable via Package Manager so it has to be installed manually.

It is recommended to install Project Auditor via the _Add package from git URL_ in Package Manager. For more information on this and alternative installation methods please refer to this [page](Installing.md).

## How to Use
The Project Auditor editor window can be open via *Window => Analysis => Project Auditor*. Click on Analyze, then review the list of potential issues to determine whether they are actual problems in your project.

For more information, check the [Getting started](GettingStarted.md) guide.

## Known limitations
Here are several Project Auditor's known limitations:

* It reports issues in code that might be stripped by the build process.
* It is unable to distinguish between cold and hot-paths.
* The call tree analysis does not support virtual methods.

## Reporting issues
If you have issues running Project Auditor in your Unity project, please report them on the [GitHub repository](https://github.com/Unity-Technologies/ProjectAuditor/issues).

## Requirements
Project Auditor is meant to be compatible with all versions of Unity, however, the latest [Long-Term Support](https://unity3d.com/unity/qa/lts-releases) version is recommended.

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
|Feb 15, 2021|Updated documentation|
|Oct 16, 2020|Added information about command line execution|
|May 21, 2020 |Expanded *Using Project Auditor* section|
|Dec 4, 2019|First draft.|
