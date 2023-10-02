using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Utils;
using Unity.ProjectAuditor.Editor.Build;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Modules
{
    enum BuildReportMetaData
    {
        Value,
        Num
    }

    enum BuildReportFileProperty
    {
        ImporterType = 0,
        RuntimeType,
        Size,
        SizePercent,
        BuildFile,
        Num
    }

    enum BuildReportStepProperty
    {
        Duration = 0,
        Message,
        Depth,
        Num
    }

    class BuildReportModule : ProjectAuditorModule
    {
        internal interface IBuildReportProvider
        {
            BuildReport GetBuildReport(BuildTarget platform);
        }

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

        static readonly IssueLayout k_MetaDataLayout = new IssueLayout
        {
            category = IssueCategory.BuildSummary,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Key" }
            }
        };

        static readonly IssueLayout k_FileLayout = new IssueLayout
        {
            category = IssueCategory.BuildFile,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Source Asset"},
                new PropertyDefinition { type = PropertyType.FileType, name = "File Type", longName = "File Extension"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildReportFileProperty.ImporterType), format = PropertyFormat.String, name = "Importer Type"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildReportFileProperty.RuntimeType), format = PropertyFormat.String, name = "Runtime Type", defaultGroup = true},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildReportFileProperty.Size), format = PropertyFormat.Bytes, name = "Size", longName = "Size in the Build"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildReportFileProperty.SizePercent), format = PropertyFormat.Percentage, name = "Size % (of Data)", longName = "Percentage of the total data size"},
                new PropertyDefinition { type = PropertyType.Path, name = "Path", hidden = true},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildReportFileProperty.BuildFile), format = PropertyFormat.String, name = "Build File"}
            }
        };

        static readonly IssueLayout k_StepLayout = new IssueLayout
        {
            category = IssueCategory.BuildStep,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.LogLevel},
                new PropertyDefinition { type = PropertyType.Description, name = "Build Step"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildReportStepProperty.Duration), format = PropertyFormat.String, name = "Duration"}
            },
            hierarchy = true
        };

        static IBuildReportProvider s_BuildReportProvider;
        static IBuildReportProvider s_DefaultBuildReportProvider = new LastBuildReportProvider();

        internal static IBuildReportProvider BuildReportProvider
        {
            get => s_BuildReportProvider != null ? s_BuildReportProvider : s_DefaultBuildReportProvider;
            set => s_BuildReportProvider = value;
        }

        internal static IBuildReportProvider DefaultBuildReportProvider => s_BuildReportProvider;

        public override string name => "Build Report";

#if !BUILD_REPORT_API_SUPPORT
        public override bool isSupported => false;
#endif

        public override IReadOnlyCollection<IssueLayout> supportedLayouts => new IssueLayout[]
        {
            k_MetaDataLayout,
            k_FileLayout,
            k_StepLayout
        };

        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
#if BUILD_REPORT_API_SUPPORT
            var buildReport = BuildReportProvider.GetBuildReport(projectAuditorParams.platform);
            if (buildReport != null)
            {
                projectAuditorParams.onIncomingIssues(new[]
                {
                    NewMetaData(k_KeyBuildPath, buildReport.summary.outputPath),
                    NewMetaData(k_KeyPlatform, buildReport.summary.platform),
                    NewMetaData(k_KeyResult, buildReport.summary.result),
                    NewMetaData(k_KeyStartTime, Formatting.FormatDateTime(buildReport.summary.buildStartedAt)),
                    NewMetaData(k_KeyEndTime, Formatting.FormatDateTime(buildReport.summary.buildEndedAt)),
                    NewMetaData(k_KeyTotalTime, Formatting.FormatDuration(buildReport.summary.totalTime)),
                    NewMetaData(k_KeyTotalSize, Formatting.FormatSize(buildReport.summary.totalSize)),
                });
                projectAuditorParams.onIncomingIssues(AnalyzeBuildSteps(buildReport));
                projectAuditorParams.onIncomingIssues(AnalyzePackedAssets(buildReport));
            }
#endif
            projectAuditorParams.onModuleCompleted?.Invoke();
        }

#if BUILD_REPORT_API_SUPPORT
        IEnumerable<ProjectIssue> AnalyzeBuildSteps(BuildReport buildReport)
        {
            foreach (var step in buildReport.steps)
            {
                var depth = step.depth;
                yield return ProjectIssue.CreateWithoutDiagnostic(IssueCategory.BuildStep, step.name)
                    .WithCustomProperties(new object[(int)BuildReportStepProperty.Num]
                    {
                        Formatting.FormatDuration(step.duration),
                        step.name,
                        depth
                    })
                    .WithSeverity(Severity.Info);

                foreach (var message in step.messages)
                {
                    var logMessage = message.content;
                    var description = new StringReader(logMessage).ReadLine(); // only take first line
                    yield return ProjectIssue.CreateWithoutDiagnostic(IssueCategory.BuildStep, description)
                        .WithCustomProperties(new object[(int)BuildReportStepProperty.Num]
                        {
                            0,
                            logMessage,
                            depth + 1
                        })
                        .WithSeverity(Diagnostic.Utils.LogTypeToSeverity(message.type));
                }
            }
        }

        IEnumerable<ProjectIssue> AnalyzePackedAssets(BuildReport buildReport)
        {
            var dataSize = buildReport.packedAssets.SelectMany(p => p.contents).Sum(c => (long)c.packedSize);

            foreach (var packedAsset in buildReport.packedAssets)
            {
                // note that there can be several entries for each source asset (for example, a prefab can reference a Texture, a Material and a shader)
                foreach (var content in packedAsset.contents)
                {
                    // sourceAssetPath might contain '|' which is invalid. This is due to compressed texture format names in the asset name such as DXT1|BC1
                    var assetPath = PathUtils.ReplaceInvalidChars(content.sourceAssetPath);
                    var assetImporter = AssetImporter.GetAtPath(assetPath);
                    var description = string.IsNullOrEmpty(assetPath) ? k_Unknown : Path.GetFileNameWithoutExtension(assetPath);

                    yield return ProjectIssue.CreateWithoutDiagnostic(IssueCategory.BuildFile, description)
                        .WithLocation(assetPath)
                        .WithCustomProperties(new object[(int)BuildReportFileProperty.Num]
                        {
                            assetImporter != null ? assetImporter.GetType().FullName : k_Unknown,
                            content.type,
                            content.packedSize,
                            Math.Round((double)content.packedSize / dataSize, 4),
                            packedAsset.shortPath
                        });
                }
            }
        }

        ProjectIssue NewMetaData(string key, object value)
        {
            return ProjectIssue.CreateWithoutDiagnostic(IssueCategory.BuildSummary, key)
                .WithCustomProperties(new object[(int)BuildReportMetaData.Num] { value });
        }

#endif
    }
}
