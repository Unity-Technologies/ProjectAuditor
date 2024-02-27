# Installing Project Auditor

The package can be installed directly from this Git repository. The instructions that follow are specific to Project Auditor, however, more information on how a package can be added to a project as a dependency from Git, you can check the Package Manager [documentation](https://docs.unity3d.com/Manual/upm-git.html).

### Package Manager UI (Recommended)
The easiest way to install Project Auditor is via Package Manager with the following steps:

Click on _Code_ and copy the repository __HTTPS__ URL to the clipboard

<img src="images/copy-repo-url.png">

In Package Manager, click on the _+ button_ (top left) and select _Add package from git URL_

<img src="images/pm-install-url.png">

Finally, paste the URL and click _Add_
 
<img src="images/pm-add-url.png">

Note that to install a specific version, simply add `#<version>` at the end of the URL. For example:

```https://github.com/Unity-Technologies/ProjectAuditor.git#0.11.0```

A list of releases can be found [here](https://github.com/Unity-Technologies/ProjectAuditor/releases).

## Upgrade to a newer version
Under the hood, the method described above adds `com.unity.project-auditor` as a dependency in the project `Packages/manifest.json` file. To upgrade to a new Project Auditor version, you can simply modify the tag. For example:

```
{
  "dependencies": {
    "com.unity.project-auditor": "https://github.com/Unity-Technologies/ProjectAuditor.git#0.8.2-preview",
  }
}
```

## Install as a tarball
If you are working in Unity 2020.3 and cannot install the package directly from the Git URL for some reason, another option is to install the package from a tarball. See the Package Manager [documentation](https://docs.unity3d.com/Manual/upm-localpath.html) about tarballs for more information.

## Installation troubleshooting
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
