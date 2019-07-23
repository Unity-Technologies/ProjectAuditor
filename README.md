# Project Analyzer
Project Analyzer is an experimental static analysis tool for Unity Projects. This tool will analyze scripts and project settings of any Unity project and report a list a possible problems that might affect performance, memory and other areas.

## Installation

### Package Manager
The easiest way to install Project Analyzer to your project is by adding it as a dependency to the project Packages/manifest.json file:

```
{
  "dependencies": {
    "com.unity.project-analyzer": "https://git@github.com/mtrive/ProjectAnalyzer.git",
  }
}
```

### Clone Repository
Alternatively, it is possible to clone the repository:

```
cd Assets
git clone https://github.com/mtrive/ProjectAnalyzer.git
```

## How to Use
The Project Analyzer editor window can be open via *Window => Analysis => Project Analyzer*.
Then click on Analyze.
