# About Project Auditor
Project Auditor is a static analysis tool for Unity projects, which finds potential issues related to memory consumption, performance and other areas.

Use the Project Auditor package to analyse scripts and settings of your project. The tool creates a list of potential issues which have been identified, then the user will need to determine whether they are real issues or not.

## Preview package
This package is available as a preview, so it is not ready for production use. The features and documentation in this package might change before it is verified for release.


# Installing Project Auditor
Project Auditor can be installed as a package in Unity 2018+, or added to the `Assets` folder in previous versions of Unity.
### Unity 2018 or newer
Add `com.unity.project-auditor` as a dependency to the project `Packages/manifest.json` file:

```
{
  "dependencies": {
    "com.unity.project-auditor": "https://git@github.com/mtrive/ProjectAuditor.git",
  }
}
```

Alternatively it's possible to clone the repository, or decompress the pre-packaged zip, to the `Packages` folder of your project.

### Unity 2017 or older
Clone this repository to your Unity project as follows:

```
cd Assets
git clone https://github.com/mtrive/ProjectAuditor.git
```

<a name="UsingProjectAuditor"></a>
# Using Project Auditor
To open the Project Auditor window in Unity, go to Window => Analysis => Project Auditor.

# Technical details
## Requirements
This version of Project Auditor is compatible with the following versions of the Unity Editor:

* 5.6 and later. However, to use it as a package 2018.1 is required.

## Known limitations
Project Auditor version 0.1.x includes the following known limitations:

* It reports issues in code that might be stripped by the build process.
* It is unable to distinguish between cold and hot-paths.

## Package contents
The following table indicates the package directory structure:

|Location|Description|
|---|---|
|`Data`|Contains the issue definition database.|
|`Documentation~`|Contains documentation files.|
|`Editor`|Contains all editor scripts: Project Auditor, Editor window and external DLLs.|
|`Tests`|Contains all scripts required to test the package.|

## Document revision history 
|Date|Reason|
|---|---|
|Dec 4, 2019|First draft.|