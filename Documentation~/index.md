# About Project Auditor
Project Auditor is a suite of static analysis tools for Unity projects. Whilst profiling tools such as the
[Unity Profiler](https://docs.unity3d.com/Manual/Profiler.html), [Frame Debugger](https://docs.unity3d.com/Manual/frame-debugger-window.html), [Memory Profiler](https://docs.unity3d.com/Packages/com.unity.memoryprofiler@latest) and [Profile Analyzer](https://docs.unity3d.com/Packages/com.unity.performance.profile-analyzer@latest) help you to understand
the runtime performance of your project by recording data at runtime, Project Auditor reports insights and issues about
the scripts, assets and settings in your project without ever needing to run it. Issues are reported alongside
actionable advice based on best practices gathered by Unity consultants and engineers. 

After analyzing your project, Project Auditor produces a report that includes the following:

* **Code Issues:** a list of possible problems that might affect performance, memory usage, Editor iteration times, and other areas.
* **Asset Issues:** Assets with import settings or file organization that may impact startup times, runtime memory usage or performance.
* **Project Settings Issues:** a list of possible problems that might affect performance, memory and other areas.
* **Build Report Insights:** How long each step of the last clean build took, and what assets were included in it.

## Requirements
Project Auditor is compatible with Unity versions from 2020.3 to the latest [Long-Term Support](https://unity3d.com/unity/qa/lts-releases) (recommended). 

<!--- TODO REMOVE THIS DISCLAIMER AS WE APPROACH RELEASE -->
## Disclaimer
This package is available as an experimental package, so it is not ready for production use. The features and
documentation in this package might change before it is verified for release. 

## Installation
See the [Installation](./Installing.md) page for installation instructions. 

## How to use
In the Unity Editor, the Project Auditor window can be opened via **Window > Analysis > Project Auditor**.

Click the **Start Analysis** button to perform analysis, or the load button to load a previously-saved
Project Report. You will be shown the Summary View for the report. From here, you can select a _View_ from the left
navigation panel to review the list of insights or potential issues to determine whether they are actual problems in
your project. Every View provides:

* A series of filters to narrow down the visible list of issues
* The ability to _Ignore_ issues which have been investigated and found not to be a problem
* The ability to export the View to a .csv file for use in build reports or automated testing

For more information, check the [Getting started](GettingStarted.md) guide.

For information on controlling the initial analysis, see [Configuring Project Auditor analysis](Configuration.md).

For information on a specific view, check the corresponding page linked to the left or in the
[Table of Contents](./TableOfContents.md)

## Document revision history
| Date             | Reason                                                                   |
|------------------|--------------------------------------------------------------------------|
| **Jan 29, 2024** | Revision including the new welcome screen, configuration and navigation. |
| **Jan 12, 2024** | Full documentation pass prior to 0.11.0 release.                         |
| **Mar 9, 2023**  | Added table of contents and updated installation instructions.           |
| **Mar 11, 2022** | Updated links to view-specific pages.                                    |
| **Jul 23, 2021** | Added view-specific pages.                                               |
| **Apr 9, 2021**  | Updated index page with more detail.                                     |
| **Feb 15, 2021** | Updated documentation.                                                   |
| **Oct 16, 2020** | Added information about command line execution.                          |
| **May 21, 2020** | Expanded *Using Project Auditor* section.                                |
| **Dec 4, 2019**  | First draft.                                                             |
