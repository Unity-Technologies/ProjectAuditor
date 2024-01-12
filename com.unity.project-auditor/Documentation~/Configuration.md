<a name="Configuration"></a>
# Configuring Project Auditor analysis

This page details the options available in Project Auditor's [Welcome View](WelcomeView.md). Select options from the
drop-down menus in this view before clicking the **Analyze** button to control aspects of the analysis process.

## Project Areas
The **Project Areas** drop-down menu is used to select the broad areas of the project to be included in project
analysis. By default, all areas are selected. If you are only interested in specific areas you can un-tick the areas
you're not interested in to save some time during the initial analysis. If you later decide to enter a View relating to
an area which hasn't been selected for analysis, you will be given the opportunity to run analysis for that area, which
will be added to the report.

The areas are as follows:
<!--- TODO Tweak this table if the view navigation layout changes -->
| Area                 | Corresponding Modules                                                                                     
|----------------------|-----------------------------------------------------------------------------------------------------------|
| **Code**             | Code Issues, Assemblies, Compiler Messages, Domain Reload                                                 |
| **Project Settings** | Project Settings Issues                                                                                   |
| **Assets**           | Assets Issues, Textures, Meshes, AudioClips, Animator Controllers, Animation Clips, Avatars, Avatar Masks |
| **Shaders**          | Shader Assets, Shader Variants, Compute Shader Variants, Shader Compiler Messages, Materials              |
| **Build**            | Build Report: Build Size, Build Report: Build Steps                                                       |

## Platform
The **Platform** drop-down menu contains all of the currently-supported platform modules included in your
installed Unity Editor. By default, the current target build platform is selected. Because Project Auditor's code
analysis compiles the assemblies in your project, this option allows you to specify the target platform for analysis,
which may be different to your current build target.

## Compilation Mode
The **Compilation Mode** drop-down menu offers further control over which assemblies to compile for code analysis, and
how to treat those compiled assemblies. The default option is **Player**. The options are as follows:

| Compilation Mode   | Description                                                                                                                                                                                                                                                                                                                                                        |
|--------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Player             | Code will be compiled for analysis as it would be when making a non-development Player build for the specified target platform. Code inside `#if DEVELOPMENT_BUILD` will be excluded from this analysis.                                                                                                                                                           |
| Development Player | Code will be compiled for analysis as it would be when making a development Player build for the specified target platform. Code inside `#if DEVELOPMENT_BUILD` will be included in this analysis.                                                                                                                                                                 |
| Editor Play Mode   | Analysis will be performed on the assemblies which are used in Play Mode. Because these assemblies are cached by the Editor, Project Auditor skips the compilation step which speeds up analysis. The analyzed code may not be completely representative of the code that would appear in a Player build, but may be a reasonable approximation for many purposes. |
| Editor             | Analysis will be performed only on Editor code assemblies. Select this option to analyze custom Editor code, including the contents of packages.                                                                                                                                                                                                                   |


