# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2020-04-27
* Added Boxing allocation analyzer
* Added Empty *MonoBehaviour* method analyzer
* Added *GameObject.tag* issue type to built-in analyzer
* Added *StaticBatchingAndHybridPackage* analyzer
* Added *Object.Instantiate* and *GameObject.AddComponent* issue types to built-in analyzer
* Added *String.Concat* issue type to built-in analyzer
* Added "experimental" allocation analyzer
* Added performance critical context analysis
* Detect *MonoBehaviour.Update/LateUpdate/FixedUpdate* as perf critical contexts
* Detect *ComponentSystem/JobComponentSystem.OnUpdat*e as perf critical contexts
* Added critical-only UI filter
* Optimized UI refresh performance and Assembly analysis
* Added profiler markers
* Added background analysis support

## [0.1.0] - 2019-11-20
* Added Config asset support
* Added Mute/Unmute buttons
* Replaced Filters checkboxes with Popups
* Added Assembly column

## [0.0.4] - 2019-10-11
* Added Calling Method information
* Added Grouped view to Script issues
* Removed "Resolved" checkboxes
* Lots of bug fixes

## [0.0.3] - 2019-09-04
* Fixed Unity 2017.x backwards compatibility
* Added Progress bar
* Added Package whitelist
* Added Tooltips

## [0.0.2] - 2019-08-22

### First usable version

*Replaced placeholder database with real issues to look for*. This version also allows the user to Resolve issues.

## [0.0.1] - 2019-07-23

### This is the first release of *Project Auditor*

*Proof of concept, mostly developed during Hackweek 2019*.
