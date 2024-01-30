<a name="Glossary"></a>
# Glossary of terms
**Analysis target:** The target platform which Project Auditor considers whilst running analysis. This can be important
because some Issues may only affect certain platforms, and compiled code may differ per-platform. By default, the
analysis target is the same as the current target platform in the
[Build Settings](https://docs.unity3d.com/Manual/BuildSettings.html), but this can be changed to a different supported
target platform in the Welcome View or by configuring an `AnalysisParams` object.

**Analyzer:** A code class which analyzes a particular aspect of a Unity project and constructs `ReportItems`
representing any Issues which were discovered. Project Auditor contains analyzers for each supported asset type,
for Project Settings, and several analyzers to detect different kinds of issues in C# scripts. Analyzers are typically
responsible for declaring and registering relevant `Descriptors` and `DiagnosticParams`, and typically implement an
`Analyze()` method to detect and report Issues.

**Area:** An aspect of a Unity project which may be affected by an Issue. Areas include things like runtime CPU or GPU
performance, memory footprints, build sizes, build times or iteration times. Not to be confused with Project Areas,
which are broad categories of analysis which can be selected in the Welcome View to specify what kinds of analysis
Project Auditor should perform.

**Build Report:** aka [BuildReport](https://docs.unity3d.com/ScriptReference/Build.Reporting.BuildReport.html). Every
time Unity creates a player build, it generates and saves a BuildReport object. When the Project Auditor package is
installed and a clean player build is performed, Project Auditor can analyze the BuildReport to display information
about all the files that were included in the build, and timing information detailing how long each step of the build
process took to execute.

**Descriptor:** An object which describes a type of issue - a potential problem in your project, along with a potential
course of action to address the problem.

**DiagnosticParams:** The name of a class in the Project Auditor package which is used to store diagnostic parameters - 
named integer values which can be used by analyzers to decide whether to report an Issue. For example, a parameter which
specifies a polygon count, used by the analyzer to report meshes which have a higher polygon count than the threshold.
Parameter values can be overridden on selected platforms, and can be viewed and edited in **Project Settings > Project
Auditor > Diagnostic Params**.

**Ignore:** Because Project Auditor cannot know all of the context behind the things it finds in a project, it can
sometimes report Issues that are false positives. For this reason, any reported Issue can be *ignored* by selecting it
in the table and clicking the Ignore button in the right-hand panel. See also: _Rules_ which are how ignored Issues are
implemented.

**Insight:** A report item discovered and reported by Project Auditor which is presented for informational purposes and
not specifically actionable. Examples of insights include details about a single asset, a compiler message, or build
step. Project Auditor presents filterable, sortable tables of insights as convenient ways to view and investigate some
aspect of your project. Insights are reported by Modules.

**Issue:** A report item discovered and reported by Project Auditor which represents a potential problem in your
project, which you may want to take action to address. Examples include calls to Unity APIs that are known to be slow or
generate garbage, or suboptimal project settings or asset import settings. Issues are reported by Analyzers.

**Report Item:** A term to refer to both Insights and Issues. In Project Auditor's code and API, items are represented
by `ReportItem` objects. A `ReportItem` with a valid `DescriptorId` is an Issue. One without a valid `DescriptorId` is
an Insight.

**Module:** One of the static analysis tools provided by Project Auditor. Project Auditor creates and manages several
Modules, each of which reports a different category of Insights. Modules may also create and run one or more Analyzers
to generate Report Items. For more information on the areas covered by Project Auditor's Modules, see the
[Configuration](Configuration.md) documentation.

**Project Auditor:** A suite of static analysis tools for Unity projects, in a package of the same name. Also, the name
of a C# object which is created to actually perform the analysis and to return a Report.

**Report:** An object which is output from Project Auditor's analysis process. It includes some information
about how the analysis was configured, some high-level summary information, and a list of `ProjectIssues` detailing
every issue and insight that was discovered by Project Auditor's analyzers.

**Rule:** A rule which can be used to change the Severity of an Issue. An individual Rule is characterised as a
`DescriptorId` identifying the general Issue, a filter identifying a specific instance, and a Severity to be applied to
matching issues. The Ignore button is implemented by setting the Severity of selected Issues to "None". Rules can be
viewed in **Project Settings > Project Auditor > Rules** and should be included in your project's version control
repository.

**Severity:** An indication of how serious or high-priority an Issue is likely to be.

**View:** A display shown in the main panel of the Project Auditor window, which shows all of the Items in a particular
`IssueCategory`.

**Welcome View:** The initial View shown when the Project Auditor is initially opened, or when a report is discarded to 
create a new one.




