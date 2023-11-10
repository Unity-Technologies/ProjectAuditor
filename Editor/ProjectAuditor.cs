using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Utils; // Required for TypeCache in Unity 2018
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// ProjectAuditor class is responsible for auditing the Unity project
    /// </summary>
    public sealed class ProjectAuditor
        : IPreprocessBuildWithReport
    {
        internal static string s_DataPath => PackagePath + "/Data";
        internal const string k_CanonicalPackagePath = "Packages/" + k_PackageName;

        internal const string k_PackageName = "com.unity.project-auditor";

        internal static string PackagePath
        {
            get
            {
                if (!string.IsNullOrEmpty(s_CachedPackagePath))
                    return s_CachedPackagePath;

                if (PackageUtils.IsClientPackage(k_PackageName))
                    s_CachedPackagePath = k_CanonicalPackagePath;
                else
                {
                    var paths = AssetDatabase.FindAssets("t:asmdef", new string[] { "Packages" }).Select(AssetDatabase.GUIDToAssetPath);
                    var asmDefPath = paths.FirstOrDefault(path => path.EndsWith("Unity.ProjectAuditor.Editor.asmdef"));
                    s_CachedPackagePath = PathUtils.GetDirectoryName(PathUtils.GetDirectoryName(asmDefPath));
                }
                return s_CachedPackagePath;
            }
        }

        internal static string PackageVersion
        {
            get
            {
                if (string.IsNullOrEmpty(s_CachedPackageVersion))
                    s_CachedPackageVersion = PackageUtils.GetClientPackageVersion(k_PackageName);
                return s_CachedPackageVersion;
            }
        }

        static string s_CachedPackagePath;
        static string s_CachedPackageVersion;
        static readonly Dictionary<string, IssueCategory> s_CustomCategories = new Dictionary<string, IssueCategory>();

        readonly List<Module> m_Modules = new List<Module>();

        /// <summary>
        /// ProjectAuditor default constructor
        /// </summary>
        public ProjectAuditor()
        {
            InitModules();
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
                    Debug.LogError($"Project Auditor [{instance.name}]: {e.Message} {e.StackTrace}");
                    continue;
                }
                m_Modules.Add(instance);
            }
        }

        /// <summary>
        /// Runs all modules that are both supported and enabled.
        /// </summary>
        /// <param name="analysisParams"> Parameters to control the audit process </param>
        /// <param name="progress"> Progress bar, if applicable </param>
        /// <returns> Generated report </returns>
        internal ProjectReport Audit(AnalysisParams analysisParams, IProgress progress = null)
        {
            ProjectReport projectReport = null;

            analysisParams.OnCompleted += result => { projectReport = result; };

            AuditAsync(analysisParams, progress);

            while (projectReport == null)
                Thread.Sleep(50);
            return projectReport;
        }

        /// <summary>
        /// Runs all modules that are both supported and enabled, using default parameters.
        /// </summary>
        /// <param name="progress"> Progress bar, if applicable </param>
        /// <returns> Generated report </returns>
        internal ProjectReport Audit(IProgress progress = null)
        {
            return Audit(new AnalysisParams(), progress);
        }

        /// <summary>
        /// Runs all modules that are both supported and enabled.
        /// </summary>
        /// <param name="analysisParams"> Parameters to control the audit process </param>
        /// <param name="progress"> Progress bar, if applicable </param>
        internal void AuditAsync(AnalysisParams analysisParams, IProgress progress = null)
        {
            if (analysisParams.Platform == BuildTarget.NoTarget)
                analysisParams.Platform = EditorUserBuildSettings.activeBuildTarget;

            var categories = analysisParams.Categories != null
                ? analysisParams.Categories
                : m_Modules
                    .Where(m => m.isEnabledByDefault)
                    .SelectMany(m => m.categories)
                    .ToArray();
            var report = analysisParams.ExistingReport;
            if (report == null)
                report = new ProjectReport(analysisParams);
            else
            {
                // incremental analysis
                var reportCategories = report.SessionInfo.Categories.ToList();
                reportCategories.AddRange(categories);
                report.SessionInfo.Categories = reportCategories.Distinct().ToArray();

                foreach (var category in categories)
                {
                    report.ClearIssues(category);
                }
            }

            var platform = analysisParams.Platform;
            if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(platform), platform))
            {
                // Error and early out if the user has request analysis of a platform which the Unity Editor doesn't have installed support for
                Debug.LogError($"Build target {platform} is not supported in this Unity Editor");
                analysisParams.OnCompleted(report);
                return;
            }

            var requestedModules = categories.SelectMany(GetModules).Distinct().ToArray();
            var supportedModules = requestedModules.Where(m => m != null && m.isSupported && CoreUtils.SupportsPlatform(m.GetType(), platform)).ToArray();

            var numModules = supportedModules.Length;
            if (numModules == 0)
            {
                // early out if, for any reason, there are no registered modules
                analysisParams.OnCompleted(report);
                return;
            }

            var logTimingsInfo = UserPreferences.LogTimingsInfo;
            var stopwatch = Stopwatch.StartNew();
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
                    OnModuleCompleted = () =>
                    {
                        var moduleEndTime = DateTime.Now;
                        if (logTimingsInfo)
                            Debug.Log($"Project Auditor module {module.name} took: " +
                                (moduleEndTime - moduleStartTime).TotalMilliseconds / 1000.0 + " seconds.");

                        report.RecordModuleInfo(module, moduleStartTime, moduleEndTime);

                        analysisParams.OnModuleCompleted?.Invoke();

                        var finished = --numModules == 0;
                        if (finished)
                        {
                            stopwatch.Stop();
                            if (logTimingsInfo)
                                Debug.Log("Project Auditor took: " + stopwatch.ElapsedMilliseconds / 1000.0f +
                                    " seconds.");

                            analysisParams.OnCompleted?.Invoke(report);
                        }
                    }
                };

                try
                {
                    module.Audit(moduleParams, progress);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Project Auditor module {module.name} failed: " + e.Message + " " + e.StackTrace);
                    moduleParams.OnModuleCompleted();
                }
            }

            if (logTimingsInfo)
                Debug.Log("Project Auditor time to interactive: " + stopwatch.ElapsedMilliseconds / 1000.0f + " seconds.");
        }

        internal T GetModule<T>() where T : Module
        {
            foreach (var module in m_Modules)
            {
                if (module is T)
                    return (T)module;
            }

            return null;
        }

        internal Module GetModule(Type t)
        {
            foreach (var module in m_Modules)
            {
                if (module.GetType() == t)
                    return module;
            }

            return null;
        }

        internal DescriptorID[] GetDiagnosticIDs()
        {
            return m_Modules.SelectMany(m => m.supportedDescriptorIDs).ToArray();
        }

        internal Module[] GetModules(IssueCategory category)
        {
            return m_Modules.Where(a => a.isSupported && a.supportedLayouts.FirstOrDefault(l => l.category == category) != null).ToArray();
        }

        internal bool IsModuleSupported(IssueCategory category)
        {
            return m_Modules.Any(a => a.isSupported && a.supportedLayouts.FirstOrDefault(l => l.category == category) != null);
        }

        /// <summary>
        /// Get all the categories which are reported by the supported modules
        /// </summary>
        /// <returns>An array of IssueCategory values</returns>
        internal IssueCategory[] GetCategories()
        {
            return m_Modules.Where(module => module.isSupported).SelectMany(m => m.categories).ToArray();
        }

        /// <summary>
        /// Get or Register a category by name. If the name argument does not match an existing category, a new category is registered.
        /// </summary>
        /// <param name="name">The name of the category</param>
        /// <returns>The category enum</returns>
        internal static IssueCategory GetOrRegisterCategory(string name)
        {
            if (!s_CustomCategories.ContainsKey(name))
                s_CustomCategories.Add(name, IssueCategory.FirstCustomCategory + s_CustomCategories.Count);
            return s_CustomCategories[name];
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

        /// <summary>
        /// Number of available built-in and registered categories
        /// </summary>
        /// <returns> Returns the number of available categories</returns>
        internal static int NumCategories()
        {
            return (int)IssueCategory.FirstCustomCategory + s_CustomCategories.Count;
        }

        /// <summary>
        /// Returns the relative callback order for callbacks. Callbacks with lower values are called before ones with higher values.
        /// </summary>
        public int callbackOrder => 0;

        /// <summary>
        /// Callback function which is called before a build is started. Performs a full audit and logs the number of issues found.
        /// </summary>
        /// <param name="report">A report containing information about the build, such as its target platform and output path.</param>
        public void OnPreprocessBuild(BuildReport report)
        {
            if (UserPreferences.AnalyzeOnBuild)
            {
                var projectReport = Audit();

                var numIssues = projectReport.NumTotalIssues;
                if (numIssues > 0)
                {
                    if (UserPreferences.FailBuildOnIssues)
                        Debug.LogError("Project Auditor found " + numIssues + " issues");
                    else
                        Debug.Log("Project Auditor found " + numIssues + " issues");
                }
            }
        }
    }
}
