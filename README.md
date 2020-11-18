# Project Auditor
Project Auditor is a static analysis tool for Unity Projects. Project Auditor analyzes scripts and settings of a Unity project and reports a list of potential problems that affect performance.

### Current Status
This project is still experimental and will likely change heavily in the future. So far this tool has only been tested with a few projects, therefore it might not work correctly depending on the version of Unity and the content of the project.

### Compatibility
All versions of Unity should be compatible, however, check the Installation instructions for more details.

### Disclaimer
Although this project is developed by Unity employees, it is not officially supported by Unity and it is not on Unity's roadmap. Feedback and requests are more than welcome, please enter them as issues.

### License
Project Auditor is licensed under the [Unity Package Distribution License](license.md) as of November 18th 2020. Before then, the MIT license was in play.

## Installation
Project Auditor can be installed as a package in Unity 2018+, or added to the `Assets` folder in previous versions of Unity.
### Unity 2018 or newer
Add `com.unity.project-auditor` as a dependency to the project `Packages/manifest.json` file:

```
{
  "dependencies": {
    "com.unity.project-auditor": "https://github.com/Unity-Technologies/ProjectAuditor.git",
  }
}
```

Alternatively it's possible to clone the repository, or decompress the pre-packaged zip, to the `Packages` folder of your project.

IMPORTANT: Project Auditor is supposed to work as a Package as of Unity 2018, therefore it should NOT live in the `Assets` folder.

### Unity 2017 or older
Clone this repository to your Unity project as follows:

```
cd Assets
git clone https://github.com/Unity-Technologies/ProjectAuditor.git
```

## How to Use
The Project Auditor editor window can be open via *Window => Analysis => Project Auditor*. Click on Analyze, then go through the list of potential issues to determine whether they are actual problems in your project.
