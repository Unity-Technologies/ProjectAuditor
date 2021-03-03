# Installing Project Auditor
There are several ways to install Project Auditor. Choose the most appropriare method based on your needs.

## Installing as read-only Package
At this time, Project Auditor is not discoverable via Package Manager so it has to be installed from its Git repository. The instructions that follow are specific to Project Auditor, however, more infromation on how a package can be added to a project as a dependency from Git, you can check the Package Manager [documentation](https://docs.unity3d.com/Manual/upm-git.html).

### Package Manager UI (Recommended)
The easiest way to install Project Auditor in Unity 2018 (or newer) is via Package Manager with the following steps:

Click on _Code_ and copy the repository URL to the clipboard

<img src="images/copy-repo-url.png">

In Package Manager, click on the _+ button_ (top left) and select _Add package from git URL_

<img src="images/pm-install-url.png">

Finally, paste the URL and click _Add_
 
<img src="images/pm-add-url.png">

Note that to install a specific version, simply add `#<version>` at the end of the URL. For example:

```https://github.com/Unity-Technologies/ProjectAuditor.git#0.4.1-preview```

A list of releases can be found [here](https://github.com/Unity-Technologies/ProjectAuditor/releases).

### Upgrade to a newer version
Under the hood, the method described above adds `com.unity.project-auditor` as a dependency in the project `Packages/manifest.json` file. To upgrade to a new Project Auditor version, you can simply modify the tag. For example:

```
{
  "dependencies": {
    "com.unity.project-auditor": "https://github.com/Unity-Technologies/ProjectAuditor.git#0.4.2-preview",
  }
}
```



## Installing for Development
* In Unity 2018 (or newer), simply _clone_ the repository to the `Packages` folder of your project.
* In Unity 2017 (or older), _clone_ the repository to the `Assets` folder of your project.
