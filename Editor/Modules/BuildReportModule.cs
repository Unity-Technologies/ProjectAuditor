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

    class BuildReportModule : Module
    {
        class BuildAnalysisContext : AnalysisContext
        {
            public BuildReport Report;
        }

        internal interface IBuildReportProvider
        {
            BuildReport GetBuildReport(BuildTarget platform);
        }

        const string k_KeyBuildPath = "Path";
        const string k_KeyPlatform = "Platform";
        const string k_KeyResult = "Result";

        const string k_KeyStartTime = "Start Time";
        const string k_KeyEndTime = "End Time";
        const string k_KeyTotalTime = "Total Time";
        const string k_KeyTotalSize = "Total Size";
        const string k_Unknown = "Unknown";

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

        public override string Name => "Build Report";


        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_MetaDataLayout,
            k_FileLayout,
            k_StepLayout
        };

        public override void Audit(AnalysisParams analysisParams, IProgress progress = null)
        {
            var buildReport = BuildReportProvider.GetBuildReport(analysisParams.Platform);
            if (buildReport != null)
            {
                var context = new BuildAnalysisContext()
                {
                    Report = buildReport
                };

                analysisParams.OnIncomingIssues(new[]
                {
                    NewMetaData(context, k_KeyBuildPath, buildReport.summary.outputPath),
                    NewMetaData(context, k_KeyPlatform, buildReport.summary.platform),
                    NewMetaData(context, k_KeyResult, buildReport.summary.result),
                    NewMetaData(context, k_KeyStartTime, Formatting.FormatDateTime(buildReport.summary.buildStartedAt)),
                    NewMetaData(context, k_KeyEndTime, Formatting.FormatDateTime(buildReport.summary.buildEndedAt)),
                    NewMetaData(context, k_KeyTotalTime, Formatting.FormatDuration(buildReport.summary.totalTime)),
                    NewMetaData(context, k_KeyTotalSize, Formatting.FormatSize(buildReport.summary.totalSize)),
                });

                analysisParams.OnIncomingIssues(AnalyzeBuildSteps(context));
                analysisParams.OnIncomingIssues(AnalyzePackedAssets(context));
            }
            analysisParams.OnModuleCompleted?.Invoke();
        }

        IEnumerable<ProjectIssue> AnalyzeBuildSteps(BuildAnalysisContext context)
        {
            foreach (var step in context.Report.steps)
            {
                var depth = step.depth;
                yield return context.CreateInsight(IssueCategory.BuildStep, step.name)
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
                    yield return context.CreateInsight(IssueCategory.BuildStep, description)
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

        IEnumerable<ProjectIssue> AnalyzePackedAssets(BuildAnalysisContext context)
        {
            var dataSize = context.Report.packedAssets.SelectMany(p => p.contents).Sum(c => (long)c.packedSize);

            foreach (var packedAsset in context.Report.packedAssets)
            {
                // note that there can be several entries for each source asset (for example, a prefab can reference a Texture, a Material and a shader)
                foreach (var content in packedAsset.contents)
                {
                    // sourceAssetPath might contain '|' which is invalid. This is due to compressed texture format names in the asset name such as DXT1|BC1
                    var assetPath = PathUtils.ReplaceInvalidChars(content.sourceAssetPath);
                    var assetImporter = AssetImporter.GetAtPath(assetPath);
                    var description = string.IsNullOrEmpty(assetPath) ? k_Unknown : Path.GetFileNameWithoutExtension(assetPath);

                    yield return context.CreateInsight(IssueCategory.BuildFile, description)
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

        ProjectIssue NewMetaData(BuildAnalysisContext context, string key, object value)
        {
            return context.CreateInsight(IssueCategory.BuildSummary, key)
                .WithCustomProperties(new object[(int)BuildReportMetaData.Num] { value });
        }
    }
}
