# Installing Project Auditor
There are several ways to install Project Auditor. Choose the most appropriate method based on your needs.
> **Note**: To install Project Auditor for development purposes, go to this [page](Developing.md) instead.

## Installing as read-only Package
At this time, Project Auditor is not discoverable via Package Manager. The installation process depends on your Unity version.

### Version 2021.1 and later

To install this package, follow the instructions for [adding a package by name](https://docs.unity3d.com/2021.1/Documentation/Manual/upm-ui-quick.html) in the Unity Editor. The package's name is `com.unity.project-auditor`.

### Version 2020.3 and earlier
The package can be installed directly from its public Git repository. The instructions that follow are specific to Project Auditor, however, more information on how a package can be added to a project as a dependency from Git, you can check the Package Manager [documentation](https://docs.unity3d.com/Manual/upm-git.html).

#### Package Manager UI (Recommended)
The easiest way to install Project Auditor in Unity 2018 (or newer) is via Package Manager with the following steps:

Click on _Code_ and copy the repository __HTTPS__ URL to the clipboard

<img src="images/copy-repo-url.png">

In Package Manager, click on the _+ button_ (top left) and select _Add package from git URL_

<img src="images/pm-install-url.png">

Finally, paste the URL and click _Add_
 
<img src="images/pm-add-url.png">

Note that to install a specific version, simply add `#<version>` at the end of the URL. For example:

```https://github.com/Unity-Technologies/ProjectAuditor.git#0.8.2-preview```

A list of releases can be found [here](https://github.com/Unity-Technologies/ProjectAuditor/releases).

#### Upgrade to a newer version
Under the hood, the method described above adds `com.unity.project-auditor` as a dependency in the project `Packages/manifest.json` file. To upgrade to a new Project Auditor version, you can simply modify the tag. For example:

```
{
  "dependencies": {
    "com.unity.project-auditor": "https://github.com/Unity-Technologies/ProjectAuditor.git#0.8.2-preview",
  }
}
```

#### Install as a tarball
If you are working in an old version of Unity (2020.3 or earlier) and cannot install the package directly from the Git URL for some reason, another option is to install the package from a tarball. See the Package Manager [documentation](https://docs.unity3d.com/Manual/upm-localpath.html) about tarballs for more information.
