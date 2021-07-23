<a name="Code"></a>
# Code View
This view reports all Script-related diagnostics. For each issue, this view also provides an explanation of the problem and a possible solution.

<img src="images/code.png">

Note that some issues are denoted as *Critical*: this indicates the issues was found in a hot-path such as a *MonoBehaviour.Update*. 

As other diagnostic views, it allows the user to filter by several criterias. These are often useful to narrow down the list of reported issues, especially on large projects.

If the user determines the reported issue is not relevant or is a false positive, it is possible to mute the selected issue(s) so they are not reported on the next analysis.
