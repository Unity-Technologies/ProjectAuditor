# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]
* Added *ProjectAuditor.NumCategories* API
* Added module-specific incremental analysis support
* Added support to disable a module by default
* Added 'Clear Selection' and 'Filter by Description' options to context menu
* Added SavePath to configuration asset
* Added [Graphics Tier](https://docs.unity3d.com/ScriptReference/Rendering.GraphicsTier.html) information to reported Shader Variants
* Added diagnostic message formatting support
* Added dependencies panel to assembly view
* Fixed reporting of assemblies not compiled due to dependencies
* Improved code diagnostic messages

## [0.7.6-preview] - 2022-04-22
* Fixed Build Report analysis 'Illegal characters in path' exception
* Fixed Shaders analysis 'Illegal characters in path' exception
* Fixed compilation warnings
* Fixed export of variants with no keywords

## [0.7.5-preview] - 2022-04-20
* Added groups support to Shaders view
* Added support for exporting Shader Variants as [Shader Variant Collection](https://docs.unity3d.com/ScriptReference/ShaderVariantCollection.html)
* Optimized call tree building and visualization

## [0.7.4-preview] - 2022-03-25
* Added *OnRenderObject* and *OnWillRenderObject* to list of MonoBehavior critical contexts
* Added Compilation Time property to Assemblies view
* Added public API to get float/double custom property 
* Added context menu item to open selected issue 
* Fixed closure allocation diagnostic message
* Fixed sorting of call hierarchy nodes
* Optimized viewing and sorting UI performance

## [0.7.3-preview] - 2022-03-01
* Added *UnityEngine.Object.name* code diagnostic
* Added *Severity* filters support
* Fixed unreported assemblies that failed to compile
* Fixed view switching if any module is unsupported
* Fixed database of API usage descriptors
* Removed redundant API usage descriptors

## [0.7.2-preview] - 2022-01-21
* Added shader *Size*, *Source Asset* and *Always Included* info to Shaders view
* Added shader *Severity* column to indicate any compiler message
* Added *Stage*, *Pass Type* and *Platform Keywords* to Shader Variants view
* Added Shader Variants view right scrollable panels
* Added Shader Compiler Messages reporting
* Fixed usage of deprecated shader API
* Fixed shader compilation log parsing in 2021 or newer
* Fixed cleanup of Shader Variants builds data in 2021 or newer

## [0.7.1-preview] - 2021-12-15
* Added option to enable creation of BuildReport asset after each build
* Fixed UWP compilation issues
* Fixed *ArgumentException* on table Page Up/Down
* Fixed *InvalidOperationException* due failure to resolve asmdef
* Fixed *NullReferenceException* due to null compiler message
* Fixed *NullReferenceException* on empty table
* Fixed *ShaderCompilerData* parsing in 2021.2.0a16 or newer
* Fixed disabling of unsupported modules
* Fixed unreported output files from the same source asset
* Fixed automatic creation of last BuildReport asset after build

## [0.7.0-preview] - 2021-11-29
* Added Documentation pages
* Added UI Button to open documentation page based on active view
* Added BuildReport Viewer UI
* Added Runtime Type property to BuildReport size items
* Added *OnAnimatorIK* and *OnAnimatorMove* to *MonoBehaviour* hot-paths
* Fixed *NullReferenceException* on projects with multiple dll with same name
* Fixed Variants view *ShaderRequirements* information
* Fixed window opening after each build

## [0.6.6-preview] - 2021-10-14
* Fixed *ProjectReport.ExportToCSV* filtering

## [0.6.5-preview] - 2021-08-04
* Fixed Mono.Cecil package dependency

## [0.6.4-preview] - 2021-07-26
* Added *ProjectReport.ExportToCSV* to *public* API
* Fixed "No graphic device is available" error in batchmode

## [0.6.3-preview] - 2021-07-05
* Fixed *NullReferenceException* when searching Call Tree on Resources view
* Fixed *OverflowException* on reporting build sizes
* Fixed Player.log parsing if a shader name contains commas
* Fixed persistent "Analysis in progress..." message

## [0.6.2-preview] - 2021-05-25
* Added Assemblies view (experimental)
* Added Build Report Steps view
* Added overview stats to Build Report Size view
* Fixed detection of HDRP mixed LitShaderMode

## [0.6.1-preview] - 2021-05-11
* Added HDRP settings analyzer
* Fixed Build Report *Build Name*
* Fixed empty MonoBehaviour event detection
* Fixed *Graphics Tier Settings* misreporting
* Fixed failed/cancelled report loading from file
* Improved Shader Variants analysis workflow

## [0.6.0-preview] - 2021-04-26
* Added Build Report support
* Added Compiler Messages support
* Added Generic types instantiation analysis
* Added Summary view
* Added Save&Load support
* Added *Log Shader Compilation* option to Shader Variants view
* Added Shaders view shortcut to Shader Variants view
* Changed compilation pipeline to use AssemblyBuilder
* Changed Shader Variants Window to simple view
* Fixed Shader Variants persistence in UI after Domain Reload
* Fixed Shader Variants *Compiled* column initial state
* Fixed Code Diagnostics view sorting
* Improved main documentation page

## [0.5.0-preview] - 2021-03-11
* Fixed reporting of issues affecting multiple areas
* Fixed background analysis that results in code issues with empty filenames
* Fixed Android player.log parsing
* Fixed *GraphicsSettings.logWhenShaderIsCompiled* compilation error on early 2018.4.x releases 
* Added *System.DateTime.Now* usage detection
* Added descriptor's minimum/maximum version
* Added splash-screen setting detection
* Added zoom slider
* Reduced UI managed allocations
* Replaced tabs-like view selection with toolbar dropdown list
* Removed *experimental* label from Allocation issues
* Changed *Export* feature to be view-specific

## [0.4.2-preview] - 2021-02-01
* Fixed detection of API calls using a derived type
* Fixed reporting of *Editor Default Resources* shaders
* Fixed *ReflectionTypeLoadException*
* Added *SRP Batcher* column to Shader tab
* Added support for parsing *Player.log* to identify which shader variants are compiled (or not-compiled) at runtime
* Added Shader errors/warnings reporting via Shader 'severity' icon
* Added [Shader Requirements](https://docs.unity3d.com/ScriptReference/Rendering.ShaderRequirements.html) column to Shader tab
* Fixed exception when switching focus from Area/Assembly window
* Fixed *NullReferenceException* on invalid shader or vfx shader
* Fixed *NullReferenceException* when building AssetBundles
* Fixed shader variants reporting due to *OnPreprocessBuild* callback default order

## [0.4.1-preview] - 2020-12-14
* Improved Shaders auditing to report both shaders and variants in their respective tables
* Added support for analyzing Editor only code-paths
* Added *reuseCollisionCallbacks* physics API diagnostic
* Fixed Assembly-CSharp-firstpass asmdef warning
* Fixed backwards compatibility

## [0.4.0-preview] - 2020-11-24
* Added Shader variants auditing
* Added "Collapse/Expand All" buttons
* Refactoring and code quality improvements

## [0.3.1-preview] - 2020-10-23
* Page up/down key bug fixes
* Added dependencies view to Assets tab
* Move call tree to the bottom of the window
* Double-click on an asset selects it in the Project Window
* Fixed Unity 2017 compatibility
* Fixed default selected assemblies
* Fixed Area names filtering
* Changed case-sensitive string search to be optional
* Added CI information to documentation
* Fixed call-tree serialization

## [0.3.0-preview] - 2020-10-07
* Added auditing of assets in Resources folders
* Added shader warmup issues
* Reorganized UI filters and mute/unmute buttons in separate foldouts
* Fixed issues sorting within a group
* ExportToCSV improvements
* Better names for project settings issues

## [0.2.1-preview] - 2020-05-22
* Improved text search UX
* Improved test coverage
* Fixed background assembly analysis
* Fixed lost issue location after domain reload
* Fix tree view selection when background analysis is enabled
* Fixed Yamato configuration
* Updated documentation

## [0.2.0-preview] - 2020-04-27
* Added Boxing allocation analyzer
* Added Empty *MonoBehaviour* method analyzer
* Added *GameObject.tag* issue type to built-in analyzer
* Added *StaticBatchingAndHybridPackage* analyzer
* Added *Object.Instantiate* and *GameObject.AddComponent* issue types to built-in analyzer
* Added *String.Concat* issue type to built-in analyzer
* Added "experimental" allocation analyzer
* Added performance critical context analysis
* Detect *MonoBehaviour.Update/LateUpdate/FixedUpdate* as perf critical contexts
* Detect *ComponentSystem/JobComponentSystem.OnUpdate* as perf critical contexts
* Added critical-only UI filter
* Optimized UI refresh performance and Assembly analysis
* Added profiler markers
* Added background analysis support

## [0.1.0-preview] - 2019-11-20
* Added Config asset support
* Added Mute/Unmute buttons
* Replaced Filters checkboxes with Popups
* Added Assembly column

## [0.0.4-preview] - 2019-10-11
* Added Calling Method information
* Added Grouped view to Script issues
* Removed "Resolved" checkboxes
* Lots of bug fixes

## [0.0.3-preview] - 2019-09-04
* Fixed Unity 2017.x backwards compatibility
* Added Progress bar
* Added Package whitelist
* Added Tooltips

## [0.0.2-preview] - 2019-08-22

### First usable version

*Replaced placeholder database with real issues to look for*. This version also allows the user to Resolve issues.

## [0.0.1-preview] - 2019-07-23

### This is the first release of *Project Auditor*

*Proof of concept, mostly developed during Hackweek 2019*.
