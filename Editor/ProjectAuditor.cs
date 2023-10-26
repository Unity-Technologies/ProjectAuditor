using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Utils;
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

        readonly List<ProjectAuditorModule> m_Modules = new List<ProjectAuditorModule>();

        /// <summary>
        /// ProjectAuditor default constructor
        /// </summary>
        public ProjectAuditor()
        {
            InitModules();
        }

        void InitModules()
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(ProjectAuditorModule)))
            {
                if (type.IsAbstract)
                    continue;
                var instance = Activator.CreateInstance(type) as ProjectAuditorModule;
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
        /// <param name="projectAuditorParams"> Parameters to control the audit process </param>
        /// <param name="progress"> Progress bar, if applicable </param>
        /// <returns> Generated report </returns>
        internal ProjectReport Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            ProjectReport projectReport = null;

            projectAuditorParams.OnCompleted += result => { projectReport = result; };

            AuditAsync(projectAuditorParams, progress);

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
            return Audit(new ProjectAuditorParams(), progress);
        }

        /// <summary>
        /// Runs all modules that are both supported and enabled.
        /// </summary>
        /// <param name="projectAuditorParams"> Parameters to control the audit process </param>
        /// <param name="progress"> Progress bar, if applicable </param>
        internal void AuditAsync(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            var categories = projectAuditorParams.Categories != null
                ? projectAuditorParams.Categories
                : m_Modules
                    .Where(m => m.isEnabledByDefault)
                    .SelectMany(m => m.categories)
                    .ToArray();
            var report = projectAuditorParams.ExistingReport;
            if (report == null)
                report = new ProjectReport(projectAuditorParams);
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

            var platform = projectAuditorParams.Platform;
            if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(platform), platform))
            {
                // Error and early out if the user has request analysis of a platform which the Unity Editor doesn't have installed support for
                Debug.LogError($"Build target {platform} is not supported in this Unity Editor");
                projectAuditorParams.OnCompleted(report);
                return;
            }

            var requestedModules = categories.SelectMany(GetModules).Distinct().ToArray();
            var supportedModules = requestedModules.Where(m => m != null && m.isSupported && CoreUtils.SupportsPlatform(m.GetType(), projectAuditorParams.Platform)).ToArray();

            var numModules = supportedModules.Length;
            if (numModules == 0)
            {
                // early out if, for any reason, there are no registered modules
                projectAuditorParams.OnCompleted(report);
                return;
            }

            var logTimingsInfo = UserPreferences.LogTimingsInfo;
            var stopwatch = Stopwatch.StartNew();
            foreach (var module in supportedModules)
            {
                var moduleStartTime = DateTime.Now;
                var moduleParams = new ProjectAuditorParams(projectAuditorParams)
                {
                    OnIncomingIssues = results =>
                    {
                        var resultsList = results.ToList();
                        report.AddIssues(resultsList);
                        projectAuditorParams.OnIncomingIssues?.Invoke(resultsList);
                    },
                    OnModuleCompleted = () =>
                    {
                        var moduleEndTime = DateTime.Now;
                        if (logTimingsInfo)
                            Debug.Log($"Project Auditor module {module.name} took: " +
                                (moduleEndTime - moduleStartTime).TotalMilliseconds / 1000.0 + " seconds.");

                        report.RecordModuleInfo(module, moduleStartTime, moduleEndTime);

                        projectAuditorParams.OnModuleCompleted?.Invoke();

                        var finished = --numModules == 0;
                        if (finished)
                        {
                            stopwatch.Stop();
                            if (logTimingsInfo)
                                Debug.Log("Project Auditor took: " + stopwatch.ElapsedMilliseconds / 1000.0f +
                                    " seconds.");

                            projectAuditorParams.OnCompleted?.Invoke(report);
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

            // Save any new DiagnosticParams that may have been declared by previously-unseen modules during analysis
            projectAuditorParams.Rules.Save();

            if (logTimingsInfo)
                Debug.Log("Project Auditor time to interactive: " + stopwatch.ElapsedMilliseconds / 1000.0f + " seconds.");
        }

        internal T GetModule<T>() where T : ProjectAuditorModule
        {
            foreach (var module in m_Modules)
            {
                if (module is T)
                    return (T)module;
            }

            return null;
        }

        internal ProjectAuditorModule GetModule(Type t)
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

        internal ProjectAuditorModule[] GetModules(IssueCategory category)
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
        /// Get the layout for a category
        /// </summary>
        /// <param name="category">The category to get the layout for</param>
        /// <returns>The IssueLayout for the specified category</returns>
        internal IssueLayout GetLayout(IssueCategory category)
        {
            if (category == IssueCategory.Metadata)
                return new IssueLayout {category = IssueCategory.Metadata, properties = new PropertyDefinition[] {}};
            var layouts = m_Modules.Where(a => a.isSupported).SelectMany(module => module.supportedLayouts).Where(l => l.category == category);
            return layouts.FirstOrDefault();
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
