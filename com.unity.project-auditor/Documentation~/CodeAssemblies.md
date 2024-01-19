<a name="Assemblies"></a>
# Assemblies View
The Assemblies View reports a list of all compiled assemblies. This includes all assemblies generated by code in the
Assets folder as well as in packages. 

The table columns are as follows:

| Column Name                | Column Description                                                                                                                                                                                                      | 
|----------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Log Level**              | The log level of the message (Error/Warning/Info). Most messages in the Assemblies View will be Info messages, but under rare circumstances assembly-level arnings or errors can be emitted by the compilation process. |                                                                                                 |
| **Assembly Name**          | The name of the assembly.                                                                                                                                                                                               |
| **Compile Time (seconds)** | The time it took to compile the assembly in preparation for code analysis when generating the report.                                                                                                                   |
| **Read Only**              | Whether the source files of an assembly can be modified in the Unity project. Packages installed from a registry or from a repository are typically *Read Only*.                                                        |
| **Asmdef path**            | The full path to the assembly definition file for this assembly.                                                                                                                                                        |
 