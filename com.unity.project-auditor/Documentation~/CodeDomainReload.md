<a name="DomainReload"></a>
# Domain Reload View

The Domain Reload View displays the results of a Roslyn analyzer that detects code issues that would result in
surprising or non-deterministic behavior if domain reload is disabled in your project. Since domain reloading can impact
project iteration times (in particular, the time it takes to enter and exit Play Mode), it's usually a very good idea to
fix all of the issues shown in this View and then to disable domain reload.

In order for the Domain Reload View to show data, you need to allow Project Auditor to run Roslyn analyzers when
analyzing your project's code. The use of Roslyn analyzers can cause Project Auditor's analysis to take longer, so it is
disabled by default. To allow Project Auditor to use Roslyn analyzers, make sure the corresponding option is enabled in
**Preferences > Analysis > Project Auditor > Use Roslyn Analyzers**.
To open the Preferences window, go to **Edit > Preferences** (macOS: **Unity > Settings**) in the main menu.

To disable Domain Reloading:

1. Go to **Edit > Project Settings > Editor**
2. Make sure Enter Play Mode Options is enabled.
3. Disable Reload Domain

## What is Domain Reload?
Domain Reloading resets the scripting state of the engine, resetting any static variables or events. By default, this is
automatically triggered when entering Play Mode, ensuring that the project will be in a completely fresh state, as it
would be when initially launched in a build. However, this comes with some performance impact, which is amplified by a
project's complexity, and also any additional behaviour that is hooked on to the Domain Reload (e.g. the contents of
functions with the InitializeOnLoad attribute in ScriptableObjects). The cumulative effect of this can cause a
significant increase in Editor iteration time, and by extension have an impact on your team's productivity.

To get around this, the Unity Editor provides the option to disable Domain Reloading. This will ensure that entering Play
Mode will always be quick. However, if not correctly reinitialised, static variables and events will retain their
values, potentially causing undesirable behaviour. This can be addressed by the addition of an initialisation function
with the RuntimeInitialiseOnLoadMethod attribute. Methods with this attribute will be called when Play Mode is entered
even if Domain Reloading is disabled, allowing you to ensure that static variables or events are initialised correctly.

For information on why you may wish to disable Domain Reloading, and what needs to be done to do so safely, see the
relevant documentation here: https://docs.unity3d.com/Manual/DomainReloading.html

## What does the Domain Reload Analyzer do?
This analyzer searches for the declaration of any static variables or events in a project. If they are found, it then
checks for the presence of a method with the `RuntimeInitializeOnLoadMethod` attribute. If such a method exists, its
contents are analyzed to determine if the static variable or event is assigned to within its scope.

Warning diagnostics are raised by the analyzer if:

* No method with the `RuntimeInitializeOnLoadMethod` attribute exists
* A variable being analyzed is not explicitly assigned within the initialize method
* An event being analyzed is not explicitly unsubscribed from within the initialize method
  
## How do I resolve these Issues?
To resolve the Issues reported in a C# script, you must do the following:

* Create an initialization function with the `RuntimeInitializeOnLoadMethod` attribute
* For every static variable in the script, ensure that it is assigned a value within the scope of the initialization function
* For every static event in the script, ensure that any functions that subscribe to the event are unsubscribed

## The View table
The table columns are as follows:

| Column Name  | Column Description                                                                                                                                                 | 
|--------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Code**     | The error/warning code reported by the compiler.                                                                                                                   |
| **Issue**    | The error/warning message string reported by the compiler. Click on an Issue to see more details in the panel on the right.                                        |
| **Filename** | The file name and line number that generated the message. Double-click on any report item to automatically open the file in your IDE and jump to the correct line. |
| **Assembly** | The assembly which contains the file that generated the message. The default assembly for user code in a Unity project is called `Assembly-CSharp`.                |
