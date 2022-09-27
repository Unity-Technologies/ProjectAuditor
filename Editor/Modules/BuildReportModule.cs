using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Audio;

namespace Unity.ProjectAuditor.Editor.Modules
{
    public enum BuildReportMetaData
    {
        Value,
        Num
    }

    public enum BuildReportFileProperty
    {
        ImporterType = 0,
        RuntimeType,
        Size,
        BuildFile,
        Num
    }

    public enum BuildReportStepProperty
    {
        Duration = 0,
        Num
    }

    public interface IBuildReportProvider
    {
        BuildReport GetBuildReport();
    }

    class LastBuildReportProvider : IBuildReportProvider
    {
        const string k_BuildReportDir = "Assets/BuildReports";
        const string k_LastBuildReportPath = "Library/LastBuild.buildreport";

        public BuildReport GetBuildReport()
        {
            return GetLastBuildReportAsset();
        }

        public static BuildReport GetLastBuildReportAsset()
        {
            if (!Directory.Exists(k_BuildReportDir))
                Directory.CreateDirectory(k_BuildReportDir);

            var date = File.GetLastWriteTime(k_LastBuildReportPath);
            var assetPath = k_BuildReportDir + "/Build_" + date.ToString("yyyy-MM-dd-HH-mm-ss") + ".buildreport";

            if (!File.Exists(assetPath))
            {
                if (!File.Exists(k_LastBuildReportPath))
                    return null; // the project was never built
                File.Copy(k_LastBuildReportPath, assetPath, true);
                AssetDatabase.ImportAsset(assetPath);
            }

            return AssetDatabase.LoadAssetAtPath<BuildReport>(assetPath);
        }
    }

    class BuildReportModule : ProjectAuditorModule
    {
#if BUILD_REPORT_API_SUPPORT
        const string k_KeyBuildPath = "Path";
        const string k_KeyPlatform = "Platform";
        const string k_KeyResult = "Result";

        const string k_KeyStartTime = "Start Time";
        const string k_KeyEndTime = "End Time";
        const string k_KeyTotalTime = "Total Time";
        const string k_KeyTotalSize = "Total Size";
        const string k_Unknown = "Unknown";
#endif

        static readonly IssueLayout k_FileLayout = new IssueLayout
        {
            category = IssueCategory.BuildFile,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Source Asset"},
                new PropertyDefinition { type = PropertyType.FileType, name = "File Type", longName = "File Extension"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildReportFileProperty.ImporterType), format = PropertyFormat.String, name = "Importer Type", defaultGroup = true},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildReportFileProperty.RuntimeType), format = PropertyFormat.String, name = "Runtime Type"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildReportFileProperty.Size), format = PropertyFormat.Bytes, name = "Size", longName = "Size in the Build"},
                new PropertyDefinition { type = PropertyType.Path, name = "Path"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildReportFileProperty.BuildFile), format = PropertyFormat.String, name = "Build File"}
            }
        };

        static readonly IssueLayout k_StepLayout = new IssueLayout
        {
            category = IssueCategory.BuildStep,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Severity},
                new PropertyDefinition { type = PropertyType.Description, name = "Build Step"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildReportStepProperty.Duration), format = PropertyFormat.String, name = "Duration"}
            },
            hierarchy = true
        };

        static IBuildReportProvider s_BuildReportProvider;
        static IBuildReportProvider s_DefaultBuildReportProvider = new LastBuildReportProvider();

        public static IBuildReportProvider BuildReportProvider
        {
            get { return s_BuildReportProvider != null ? s_BuildReportProvider : s_DefaultBuildReportProvider;  }
            set { s_BuildReportProvider = value;  }
        }

        public static IBuildReportProvider DefaultBuildReportProvider
        {
            get { return s_BuildReportProvider; }
        }

        public override string name => "Build Report";

#if !BUILD_REPORT_API_SUPPORT
        public override bool isSupported => false;
#endif

        public override IReadOnlyCollection<IssueLayout> supportedLayouts => new IssueLayout[]
        {
            k_FileLayout,
            k_StepLayout
        };

        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
#if BUILD_REPORT_API_SUPPORT
            var buildReport = BuildReportProvider.GetBuildReport();
            if (buildReport != null)
            {
                var issues = new List<ProjectIssue>();
                NewMetaData(k_KeyBuildPath, buildReport.summary.outputPath, issues);
                NewMetaData(k_KeyPlatform, buildReport.summary.platform, issues);
                NewMetaData(k_KeyResult, buildReport.summary.result, issues);
                NewMetaData(k_KeyStartTime, buildReport.summary.buildStartedAt, issues);
                NewMetaData(k_KeyEndTime, buildReport.summary.buildEndedAt, issues);
                NewMetaData(k_KeyTotalTime, Formatting.FormatBuildTime(buildReport.summary.totalTime), issues);
                NewMetaData(k_KeyTotalSize, Formatting.FormatSize(buildReport.summary.totalSize), issues);

                AnalyzeBuildSteps(buildReport, issues);
                AnalyzePackedAssets(buildReport, issues);

                if (issues.Any())
                    projectAuditorParams.onIncomingIssues(issues);
            }
#endif
            projectAuditorParams.onModuleCompleted?.Invoke();
        }

#if BUILD_REPORT_API_SUPPORT
        void AnalyzeBuildSteps(BuildReport buildReport, IList<ProjectIssue> issues)
        {
            foreach (var step in buildReport.steps)
            {
                var depth = step.depth;
                issues.Add(ProjectIssue.Create(IssueCategory.BuildStep, step.name)
                    .WithCustomProperties(new object[(int)BuildReportStepProperty.Num]
                    {
                        Formatting.FormatBuildTime(step.duration)
                    })
                    .WithDepth(depth)
                    .WithSeverity(Rule.Severity.Info));

                foreach (var message in step.messages)
                {
                    var issue = ProjectIssue.Create(IssueCategory.BuildStep, message.content)
                        .WithDepth(depth + 1)
                        .WithSeverity(LogTypeToSeverity(message.type));
                    issues.Add(issue);
                }
            }
        }

        void AnalyzePackedAssets(BuildReport buildReport, IList<ProjectIssue> issues)
        {
            foreach (var packedAsset in buildReport.packedAssets)
            {
                // note that there can be several entries for each source asset (for example, a prefab can reference a Texture, a Material and a shader)
                foreach (var content in packedAsset.contents)
                {
                    // sourceAssetPath might contain '|' which is invalid. This is due to compressed texture format names in the asset name such as DXT1|BC1
                    var assetPath = PathUtils.ReplaceInvalidChars(content.sourceAssetPath);

                    // handle special case of Built-in assets
                    if (assetPath.StartsWith("Built-in") && assetPath.Contains(":"))
                        assetPath = assetPath.Substring(0, assetPath.IndexOf(':'));

                    var description = string.IsNullOrEmpty(assetPath) ? k_Unknown : Path.GetFileNameWithoutExtension(assetPath);
                    var assetImporter = AssetImporter.GetAtPath(assetPath);
                    var issue = ProjectIssue.Create(IssueCategory.BuildFile, description)
                        .WithLocation(assetPath)
                        .WithCustomProperties(new object[(int)BuildReportFileProperty.Num]
                        {
                            assetImporter != null ? assetImporter.GetType().FullName : k_Unknown,
                            content.type,
                            content.packedSize,
                            packedAsset.shortPath
                        });
                    issues.Add(issue);
                }
            }
        }

        void NewMetaData(string key, object value, IList<ProjectIssue> issues)
        {
            var issue = ProjectIssue.Create(IssueCategory.BuildSummary, key)
                .WithCustomProperties(new object[(int)BuildReportMetaData.Num] { value });
            issues.Add(issue);
        }

        Rule.Severity LogTypeToSeverity(LogType logType)
        {
            switch (logType)
            {
                case LogType.Assert:
                case LogType.Error:
                case LogType.Exception:
                    return Rule.Severity.Error;
                case LogType.Warning:
                    return Rule.Severity.Warning;
                default:
                    return Rule.Severity.Info;
            }
        }

#endif
    }
}
