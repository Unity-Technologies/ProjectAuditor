# About Project Auditor
Project Auditor is a suite of static analysis tools for Unity projects. Whilst profiling tools such as the [Unity Profiler](https://docs.unity3d.com/Manual/Profiler.html), [Frame Debugger](https://docs.unity3d.com/Manual/frame-debugger-window.html), [Memory Profiler](https://docs.unity3d.com/Packages/com.unity.memoryprofiler@latest) and [Profile Analyzer](https://docs.unity3d.com/Packages/com.unity.performance.profile-analyzer@latest) help you to understand the runtime performance of your project by recording data at runtime, Project Auditor reports insights and issues about the scripts, assets and settings in your project without ever needing to run it. Issues are reported alongside actionable advice based on best practices gathered by Unity consultants and engineers. 

After analyzing your project, Project Auditor produces a report that includes the following:

* **Code Issues:** a list of possible problems that might affect performance, memory usage, Editor iteration times, and other areas.
* **Asset Issues:** Assets with import settings or file organization that may impact startup times, runtime memory usage or performance.
* **Project Settings Issues:** a list of possible problems that might affect performance, memory and other areas.
* **Build Report Insights:** How long each step of the last clean build took, and what assets were included in it.

## Requirements
Project Auditor is compatible with Unity versions from 2020.3 to the latest [Long-Term Support](https://unity3d.com/unity/qa/lts-releases) (recommended). 

<!--- TODO REMOVE THIS DISCLAIMER AS WE APPROACH RELEASE -->
## Disclaimer
This package is available as an experimental package, so it is not ready for production use. The features and documentation in this package might change before it is verified for release. 

## Installation

To install this package, refer to the instructions that match your Unity Editor version. In either case, when prompted for the package name, use `com.unity.project-auditor`. 

### Version 2021.1 and later

To install this package, follow the instructions for [adding a package by name](https://docs.unity3d.com/2021.1/Documentation/Manual/upm-ui-quick.html) in the Unity Editor.

### Version 2020.3

To install this package, follow the instructions for [installing hidden packages](https://docs.unity3d.com/Packages/Installation/manual/upm-ui-quick.html). 

### Installation troubleshooting
Under rare and specific circumstances, installing the Project Auditor package may result in a console error similar to
the following:

```
error CS0433: The type 'MethodAttributes' exists in both 'Mono.Cecil, Version=0.11.4.0, Culture=neutral, PublicKeyToken=50cebf1cceb9d05e'
and 'Unity.Burst.Cecil, Version=0.10.0.0, Culture=neutral, PublicKeyToken=fc15b93552389f74'
```
Project Auditor uses a library called
[Mono.Cecil](https://www.mono-project.com/docs/tools+libraries/libraries/Mono.Cecil/) in order to perform static
analysis of C# code. Project Auditor specifies Mono.Cecil as a dependency, meaning that Mono.Cecil is automatically
installed alongside the Project Auditor package, unless some other package has already installed it as a dependency.
This allows multiple packages that use Mono.Cecil to coexist in a Unity project. However, some older versions of other
Unity packages include Mono.Cecil directly rather than as a dependency. If these package versions are installed in a
project, and if any user code assemblies also make explicit use of Mono.Cecil, namespace clashes can occur. The error
message above was generated from a project which included Burst 1.8.3 and the following code in a user script:

```
using MethodAttributes = Mono.Cecil.MethodAttributes;
```

The solution in this situation is to either update Burst to 1.8.4 or above, or to remove any user code which uses
Mono.Cecil.

## How to use
In the Unity Editor, the Project Auditor window can be opened via **Window > Analysis > Project Auditor**.

<!--- TODO - change this if we switch navigation from tabs to a sidebar -->
The initial view contains configuration options to control the project areas which will be analyzed, the target platform
for analysis, Click the Analyze button to perform analysis, or the load button to load a previously-saved Project
Report. You will be shown the Summary View for the report. From here, you can click an area tab and select a _View_ from
the drop-down menu to review the list of insights or potential issues to determine whether they are actual problems in
your project. Every View provides:

* A series of filters to narrow down the visible list of issues
* The ability to _Ignore_ issues which have been investigated and found not to be a problem
* The ability to export the View to a .csv file for use in build reports or automated testing

For more information, check the [Getting started](GettingStarted.md) guide.

For information on controlling the initial analysis, see [Configuring Project Auditor analysis](Configuration.md).

For information on a specific view, check the corresponding page linked to the left or in the
[Table of Contents](./TableOfContents.md)

## Document revision history
| Date             | Reason                                                         |
|------------------|----------------------------------------------------------------|
| **Jan 12, 2024** | Full documentation pass prior to 0.11.0 release.               |
| **Mar 9, 2023**  | Added table of contents and updated installation instructions. |
| **Mar 11, 2022** | Updated links to view-specific pages.                          |
| **Jul 23, 2021** | Added view-specific pages.                                     |
| **Apr 9, 2021**  | Updated index page with more detail.                           |
| **Feb 15, 2021** | Updated documentation.                                         |
| **Oct 16, 2020** | Added information about command line execution.                |
| **May 21, 2020** | Expanded *Using Project Auditor* section.                      |
| **Dec 4, 2019**  | First draft.                                                   |
