|**Package Status**|
|:---|
|[![codecov](https://codecov.unity3d.com/github_enterprise/unity/ProjectAuditor/graph/badge.svg?token=XHRDV5PRRP)](https://codecov.unity3d.com/github_enterprise/unity/ProjectAuditor)|

# Project Auditor
Project Auditor is a static analysis tool that analyzes assets, settings, and scripts of the Unity project and produces a report containing: Code and Settings diagnostics, the last BuildReport, and assets information.

## Disclaimer
This package is available as an experimental package, so it is not ready for production use. The features and documentation in this package might change before it is verified for release. 

Feedback, bug reports and feature requests are more than welcome, please enter them [here](https://github.com/Unity-Technologies/ProjectAuditor/issues).

## Installation

To install this package, refer to the instructions that match your Unity Editor version: 

### Version 2021.1 and later

To install this package, follow the instructions for [adding a package by name](https://docs.unity3d.com/2021.1/Documentation/Manual/upm-ui-quick.html) in the Unity Editor.

The package's name is **com.unity.project-auditor** .

### Version 2020.3 and earlier

To install this package, follow the instructions for [installing hidden packages](https://docs.unity3d.com/Packages/Installation/manual/upm-ui-quick.html). 

## License
Project Auditor is licensed under the [Unity Package Distribution License](LICENSE.md) as of November 18th 2020. Before then, the MIT license was in play.

## Documentation
For information on how to install and use Project Auditor, please refer to the [documentation](Documentation~/index.md).

## Development
Here are the steps required to setup a Unity project for Project Auditor development.

### Package
The simplest way to install Project Auditor for Development purposes is to _clone_ the repository to the `Packages` folder of your project.
Alternatively, it is possible to clone the repository to a different folder. However, note that in this case you will have to add `com.unity.project-auditor` to your `Packages/manifest.json`.

### Tests
In order to be able to run (existing or new) tests in VS/Rider or within Unity using TestRunner, it is necessary to:
- Install `Test Framework` from Package Manager.
- Add the following lines to your `Packages/manifest.json`:
```
 "testables": [
    "com.unity.project-auditor"
  ]
```

### Pull Requests
1. Start a new pull request on [GitHub](https://github.com/Unity-Technologies/ProjectAuditor/compare).
2. Describe the problem you are trying to solve.
3. Describe the solution you are trying to implement.
4. Add [mtrive](https://github.com/mtrive) as reviewer to the pull request.
5. Create the pull request.
6. Once approved, select `Squash and merge` and delete the remote branch.
> **Note**: Make sure to select `Unity-Technologies/ProjectAuditor` as base repository and `master` as base branch when creating the Pull Request.

The repository owners are committed to maintaining this repository and ensuring that it continues to adhere to Unity Standards for package development. Individual pull requests may take time to be approved if they contain changes that require the package to be re-validated.
