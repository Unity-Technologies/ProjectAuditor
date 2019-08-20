# Project Analyzer
Project Analyzer is an experimental static analysis tool for Unity Projects. This tool will analyze scripts and project settings of any Unity project and report a list a possible problems that might affect performance, memory and other areas.

### Current Status
This project is still experimental and will likely change heavily in the future.

### Testing
At the moment this tool has only been tested with a few projects, therefore it might not work correctly depending on the version of Unity and the content of the project. 

## Installation

### Package Manager
The easiest way to install Project Analyzer in your Unity project is by adding it as a dependency to the project Packages/manifest.json file:

```
{
  "dependencies": {
    "com.unity.project-analyzer": "https://git@github.com/mtrive/ProjectAnalyzer.git",
  }
}
```

### Clone Repository
Alternatively, it is possible to clone the repository into the project Packages directory:

```
cd Packages
git clone https://github.com/mtrive/ProjectAnalyzer.git
```

## How to Use
The Project Analyzer editor window can be open via *Window => Analysis => Project Analyzer*.
Then click on Analyze.
