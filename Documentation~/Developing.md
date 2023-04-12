# Developing Project Auditor
Here are the steps required to setup a Unity project for Project Auditor development.

## Package
The simplest way to install Project Auditor for Development purposes is to _clone_ the repository to the `Packages` folder of your project.
Alternatively, it is possible to clone the repository to a different folder. However, note that in this case you will have to add `com.unity.project-auditor` to your `Packages/manifest.json`.

## Tests
In order to be able to run (existing or new) tests in VS/Rider or within Unity using TestRunner, it is necessary to: 
- Install `Test Framework` from Package Manager. 
- Add the following lines to your `Packages/manifest.json`:
```
 "testables": [
    "com.unity.project-auditor"
  ]
```

## Pull Requests
1. Start a new pull request on [GitHub](https://github.com/Unity-Technologies/ProjectAuditor/compare).
2. Describe the problem you are trying to solve.
3. Describe the solution you are trying to implement.
4. Add [mtrive](https://github.com/mtrive) as reviewer to the pull request.
5. Create the pull request.
6. Once approved, select `Squash and merge` and delete the remote branch.
> **Note**: Make sure to select `Unity-Technologies/ProjectAuditor` as base repository and `master` as base branch when creating the Pull Request.

The repository owners are committed to maintaining this repository and ensuring that it continues to adhere to Unity Standards for package development. Individual pull requests may take time to be approved if they contain changes that require the package to be re-validated.
