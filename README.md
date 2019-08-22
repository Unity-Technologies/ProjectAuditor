# Project Auditor
Project Auditor is an experimental static analysis tool for Unity Projects. This tool will analyze scripts and project settings of any Unity project and report a list a possible problems that might affect performance, memory and other areas.

### Current Status
This project is still experimental and will likely change heavily in the future.

### Testing
At the moment this tool has only been tested with a few projects, therefore it might not work correctly depending on the version of Unity and the content of the project. 

### Disclaimer
Although this project is developed by Unity employees, it is not officially supported by Unity and it is not on Unity's roadmap. Feedback and requests are more than welcome, please enter them as issues.

## Installation

### Package Manager
The easiest way to install Project Auditor in your Unity project is by adding it as a dependency to the project Packages/manifest.json file:

```
{
  "dependencies": {
    "com.unity.project-auditor": "https://git@github.com/mtrive/ProjectAuditor.git",
  }
}
```

### Clone Repository
Alternatively, it is possible to clone the repository into the project Packages directory:

```
cd Packages
git clone https://github.com/mtrive/ProjectAuditor.git
```

## How to Use
The Project Auditor editor window can be open via *Window => Analysis => Project Auditor*.
Then click on Analyze.
