using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using Debug = UnityEngine.Debug;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// The ProjectAuditor class is responsible for auditing the Unity project.
    /// </summary>
    public sealed class ProjectAuditor : IPostprocessBuildWithReport
    {
        /// <summary>
        /// Returns the relative callback order for callbacks. Callbacks with lower values are called before ones with higher values.
        /// </summary>
        public int callbackOrder => 1;  // We want LastBuildReportProvider to update its cached report before we run analysis.

        internal static string s_DataPath => ProjectAuditorPackage.Path + "/Data";

        internal const string DisplayName = "Project Auditor";

        internal static string ProjectPath
        {
            get
            {
                if (string.IsNullOrEmpty(s_CachedProjectPath))
                    s_CachedProjectPath = PathUtils.GetDirectoryName(UnityEngine.Application.dataPath);
                return s_CachedProjectPath;
            }
        }

        static string s_CachedProjectPath;
        static readonly Dictionary<string, IssueCategory> s_CustomCategories = new Dictionary<string, IssueCategory>();

        readonly List<Module> m_Modules = new List<Module>();

        /// <summary>
        /// ProjectAuditor default constructor
        /// </summary>
        public ProjectAuditor()
        {
            InitModules();
        }

        /// <summary>
        /// Performs static analysis of the project, using default parameters.
        /// </summary>
        /// <param name="progress"> Progress bar, if applicable </param>
        /// <returns> Generated report </returns>
        public Report Audit(IProgress progress = null)
        {
            return Audit(new AnalysisParams(), progress);
        }

        /// <summary>
        /// Performs static analysis of the project, using the supplied analysis parameters.
        /// </summary>
        /// <param name="analysisParams"> Parameters to control the audit process </param>
        /// <param name="progress"> Progress bar, if applicable </param>
        /// <returns> Generated report </returns>
        public Report Audit(AnalysisParams analysisParams, IProgress progress = null)
        {
            Report report = null;

            analysisParams.OnCompleted += result => { report = result; };

            AuditAsync(analysisParams, progress);

            while (report == null)
                Thread.Sleep(50);
            return report;
        }

        /// <summary>
        /// Performs asynchronous static analysis of the project, using the supplied analysis parameters.
        /// Provide a callback to the `OnCompleted` Action in analysisParams to obtain the <seealso cref="Report"/> when analysis is completed.
        /// </summary>
        /// <param name="analysisParams"> Parameters to control the audit process </param>
        /// <param name="progress"> Progress bar, if applicable </param>
        public void AuditAsync(AnalysisParams analysisParams, IProgress progress = null)
        {
            if (analysisParams.Platform == BuildTarget.NoTarget)
                analysisParams.Platform = EditorUserBuildSettings.activeBuildTarget;

            var categories = analysisParams.Categories != null
                ? analysisParams.Categories
                : m_Modules
                    .SelectMany(m => m.Categories)
                    .ToArray();
            var report = analysisParams.ExistingReport;
            if (report == null)
                report = new Report(analysisParams);
            else
            {
                // incremental analysis
                var reportCategories = report.SessionInfo.Categories.ToList();
                reportCategories.AddRange(categories);
                report.SessionInfo.Categories = reportCategories.Distinct().ToArray();
                report.SessionInfo.UseRoslynAnalyzers = UserPreferences.UseRoslynAnalyzers;

                foreach (var category in categories)
                {
                    report.ClearIssues(category);
                }
            }

            var platform = analysisParams.Platform;
            if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(platform), platform))
            {
                // Error and early out if the user has request analysis of a platform which the Unity Editor doesn't have installed support for
                Debug.LogError($"[{ProjectAuditor.DisplayName}] Build target {platform} is not supported in this Unity Editor");
                analysisParams.OnCompleted(report);
                return;
            }

            var requestedModules = categories.SelectMany(GetModules).Distinct().ToArray();
            var supportedModules = requestedModules.Where(m => m != null && CoreUtils.SupportsPlatform(m.GetType(), platform)).ToArray();

            var numModules = supportedModules.Length;
            if (numModules == 0)
            {
                // early out if, for any reason, there are no registered Modules
                analysisParams.OnCompleted(report);
                return;
            }

            var logTimingsInfo = UserPreferences.LogTimingsInfo;
            var stopwatch = Stopwatch.StartNew();
            var isCancelled = false;
            foreach (var module in supportedModules)
            {
                var moduleStartTime = DateTime.Now;
                var moduleParams = new AnalysisParams(analysisParams)
                {
                    OnIncomingIssues = results =>
                    {
                        var resultsList = results.ToList();
                        report.AddIssues(resultsList);
                        analysisParams.OnIncomingIssues?.Invoke(resultsList);
                    },
                    OnModuleCompleted = (analysisResult) =>
                    {
                        var moduleEndTime = DateTime.Now;
                        if (analysisResult == AnalysisResult.Cancelled)
                            isCancelled = true;
                        else if (analysisResult == AnalysisResult.Failure)
                            Debug.Log($"[{ProjectAuditor.DisplayName}] Module {module.Name} failed.");
                        if (logTimingsInfo)
                            Debug.Log($"[{ProjectAuditor.DisplayName}] Module {module.Name} analysis took: " +
                                (moduleEndTime - moduleStartTime).TotalMilliseconds / 1000.0 + " seconds.");

                        report.RecordModuleInfo(module, moduleStartTime, moduleEndTime, analysisResult);

                        analysisParams.OnModuleCompleted?.Invoke(analysisResult);

                        var finished = --numModules == 0;
                        if (finished)
                        {
                            stopwatch.Stop();
                            if (isCancelled)
                                Debug.Log($"[{ProjectAuditor.DisplayName}] Analysis was cancelled by the user.");
                            if (logTimingsInfo)
                                Debug.Log($"[{ProjectAuditor.DisplayName}] Analysis took: " + stopwatch.ElapsedMilliseconds / 1000.0f +
                                    " seconds.");

                            // finally, call the user's OnCompleted callback
                            analysisParams.OnCompleted?.Invoke(report);
                        }
                    }
                };

                try
                {
                    var analysisResult = module.Audit(moduleParams, progress);
                    if (analysisResult == AnalysisResult.Cancelled)
                        progress.Clear();
                    if (analysisResult != AnalysisResult.InProgress)
                        moduleParams.OnModuleCompleted(analysisResult);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[{ProjectAuditor.DisplayName}] Module {module.Name} failed: " + e.Message + " " + e.StackTrace);
                    moduleParams.OnModuleCompleted(AnalysisResult.Failure);
                }
            }

            if (logTimingsInfo)
                Debug.Log($"[{ProjectAuditor.DisplayName}] Time to interactive: " + stopwatch.ElapsedMilliseconds / 1000.0f + " seconds.");
        }

        /// <summary>
        /// Callback function which is called after a build is completed.
        /// If UserPreferences.AnalyzeAfterBuild is true, performs a full audit and logs the number of issues found.
        /// </summary>
        /// <param name="report">A report containing information about the build, such as its target platform and output path.</param>
        public void OnPostprocessBuild(BuildReport report)
        {
            if (UserPreferences.AnalyzeAfterBuild)
            {
                // Library/LastBuild.buildreport is only created AFTER OnPostprocessBuild so we need to defer analysis until the file is copied.
                EditorApplication.update += DelayedPostBuildAudit;
            }
        }

        internal void DelayedPostBuildAudit()
        {
            var report = Audit();

            var numIssues = report.NumTotalIssues;
            if (numIssues > 0)
            {
                if (UserPreferences.FailBuildOnIssues)
                    Debug.LogError($"[{ProjectAuditor.DisplayName}] Analysis found " + numIssues + " issues");
                else
                    Debug.Log($"[{ProjectAuditor.DisplayName}] Analysis found " + numIssues + " issues");
            }

            EditorApplication.update -= DelayedPostBuildAudit;
        }

        /// <summary>
        /// Registers an IssueCategory by name, and returns its value.
        /// </summary>
        /// <remarks>
        /// It's possible to extend Project Auditor's analysis capabilities without modifying the package, by adding new custom Modules and Analyzers to your project code.
        /// Custom analyzers may wish to report entirely new issue categories. This method is how those new categories are declared for use.
        /// </remarks>
        /// <param name="report">A custom category name.</param>
        /// <returns>A value representing the custom category.</returns>
        internal static IssueCategory GetOrRegisterCategory(string name)
        {
            if (!s_CustomCategories.ContainsKey(name))
                s_CustomCategories.Add(name, IssueCategory.FirstCustomCategory + s_CustomCategories.Count);
            return s_CustomCategories[name];
        }

        internal Module[] GetModules(IssueCategory category)
        {
            return m_Modules.Where(a => a.SupportedLayouts.FirstOrDefault(l => l.Category == category) != null).ToArray();
        }

        /// <summary>
        /// Get the name of a category
        /// </summary>
        /// <param name="category">The category to get the name of</param>
        /// <returns>The category name, or "Unknown" for an unregistered custom category</returns>
        internal static string GetCategoryName(IssueCategory category)
        {
            if (category < IssueCategory.FirstCustomCategory)
                return category.ToString();

            foreach (var pair in s_CustomCategories)
            {
                if (pair.Value == category)
                    return pair.Key;
            }

            return "Unknown";
        }

        void InitModules()
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(Module)))
            {
                if (type.IsAbstract)
                    continue;
                var instance = Activator.CreateInstance(type) as Module;
                try
                {
                    instance.Initialize();
                }
                catch (Exception e)
                {
                    Debug.LogError($"{DisplayName} [{instance.Name}]: {e.Message} {e.StackTrace}");
                    continue;
                }
                m_Modules.Add(instance);
            }
        }

        // Only used for testing
        internal DescriptorId[] GetDescriptorIDs()
        {
            return m_Modules.SelectMany(m => m.SupportedDescriptorIds).ToArray();
        }

        // Only used for testing
        internal bool IsModuleSupported(IssueCategory category)
        {
            return m_Modules.Any(a => a.SupportedLayouts.FirstOrDefault(l => l.Category == category) != null);
        }

        // Only used for testing
        internal static int NumCategories()
        {
            return (int)IssueCategory.FirstCustomCategory + s_CustomCategories.Count;
        }
    }
}
