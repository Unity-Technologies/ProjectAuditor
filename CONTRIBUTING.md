# Contributing

## If you are interested in contributing, here are some ground rules:
* Unless you are solving a trivial issue, it would be best to include a link to a discussion.
* Help the reviewers understand your work and minimize merge conflicts:
  * Fix only one thing per pull request. Make a second pull request if you have two fixes.
  * Try to minimize stylistic changes in your pull request. Don't change indentation and whitespace unless it's important.
* With the exception of UI code, make sure your changes are covered by tests.
* Make sure all tests pass.
* Make sure Project Auditor works as expected in the last LTS version. When doing so, make sure the analysis information is preserved after a Domain Reload.
* Please include entries to the changelog for any PR
* New logs should be placed under the ## [Unreleased] header at the top of the changelog.

```
# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]
* Fixed export to CSV

## [0.3.1] - 2020-10-23
* Page up/down key bug fixes

```

### Once you are ready, make a pull request!

## All contributions are subject to the [Unity Contribution Agreement(UCA)](https://unity3d.com/legal/licenses/Unity_Contribution_Agreement)
By making a pull request, you are confirming agreement to the terms and conditions of the UCA, including that your Contributions are your original creation and that you have complete right and authority to make your Contributions.
