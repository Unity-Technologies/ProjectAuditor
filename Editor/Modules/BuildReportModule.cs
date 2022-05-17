using System;
using System.Collections.Generic;
using System.IO;
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

        static readonly ProblemDescriptor k_Descriptor = new ProblemDescriptor
            (
            600000,
            "Build Meta Data"
            );
#endif

        static readonly ProblemDescriptor k_InfoDescriptor = new ProblemDescriptor
            (
            600001,
            "Build step info"
            )
        {
            severity = Rule.Severity.Info
        };

        static readonly ProblemDescriptor k_WarnDescriptor = new ProblemDescriptor
            (
            600002,
            "Build step warning"
            )
        {
            severity = Rule.Severity.Warning
        };

        static readonly ProblemDescriptor k_ErrorDescriptor = new ProblemDescriptor
            (
            600003,
            "Build step error"
            )
        {
            severity = Rule.Severity.Error
        };

        static readonly IssueLayout k_FileLayout = new IssueLayout
        {
            category = IssueCategory.BuildFile,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Source Asset"},
                new PropertyDefinition { type = PropertyType.FileType, name = "Ext"},
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

        public override IEnumerable<ProblemDescriptor> GetDescriptors()
        {
            yield return k_InfoDescriptor;
            yield return k_WarnDescriptor;
            yield return k_ErrorDescriptor;
        }

        public override IEnumerable<IssueLayout> GetLayouts()
        {
            yield return k_FileLayout;
            yield return k_StepLayout;
        }

#if !BUILD_REPORT_API_SUPPORT
        public override bool IsSupported()
        {
            return false;
        }

#endif

        public override void Audit(Action<ProjectIssue> onIssueFound, Action onComplete = null, IProgress progress = null)
        {
#if BUILD_REPORT_API_SUPPORT
            var buildReport = BuildReportProvider.GetBuildReport();
            if (buildReport != null)
            {
                NewMetaData(k_KeyBuildPath, buildReport.summary.outputPath, onIssueFound);
                NewMetaData(k_KeyPlatform, buildReport.summary.platform, onIssueFound);
                NewMetaData(k_KeyResult, buildReport.summary.result, onIssueFound);
                NewMetaData(k_KeyStartTime, buildReport.summary.buildStartedAt, onIssueFound);
                NewMetaData(k_KeyEndTime, buildReport.summary.buildEndedAt, onIssueFound);
                NewMetaData(k_KeyTotalTime, Formatting.FormatBuildTime(buildReport.summary.totalTime), onIssueFound);
                NewMetaData(k_KeyTotalSize, Formatting.FormatSize(buildReport.summary.totalSize), onIssueFound);

                AnalyzeBuildSteps(onIssueFound, buildReport);
                AnalyzePackedAssets(onIssueFound, buildReport);
            }
#endif
            if (onComplete != null)
                onComplete();
        }

#if BUILD_REPORT_API_SUPPORT
        void AnalyzeBuildSteps(Action<ProjectIssue> onIssueFound, BuildReport buildReport)
        {
            foreach (var step in buildReport.steps)
            {
                var depth = step.depth;
                onIssueFound(new ProjectIssue(k_InfoDescriptor, step.name, IssueCategory.BuildStep, new object[(int)BuildReportStepProperty.Num]
                {
                    Formatting.FormatBuildTime(step.duration)
                })
                    {
                        depth = depth
                    });

                foreach (var message in step.messages)
                {
                    var descriptor = k_InfoDescriptor;
                    switch (message.type)
                    {
                        case LogType.Assert:
                        case LogType.Error:
                        case LogType.Exception:
                            descriptor = k_ErrorDescriptor;
                            break;
                        case LogType.Warning:
                            descriptor = k_WarnDescriptor;
                            break;
                    }
                    onIssueFound(new ProjectIssue(descriptor, message.content, IssueCategory.BuildStep)
                    {
                        depth = depth + 1,
                    });
                }
            }
        }

        void AnalyzePackedAssets(Action<ProjectIssue> onIssueFound, BuildReport buildReport)
        {
            foreach (var packedAsset in buildReport.packedAssets)
            {
                // note that there can be several entries for each source asset (for example, a prefab can reference a Texture, a Material and a shader)
                foreach (var content in packedAsset.contents)
                {
                    // sourceAssetPath might contain '|' which is invalid. This is due to compressed texture format names in the asset name such as DXT1|BC1
                    var assetPath = PathUtils.ReplaceInvalidChars(content.sourceAssetPath);
                    var assetImporter = AssetImporter.GetAtPath(assetPath);
                    var description = string.IsNullOrEmpty(assetPath) ? k_Unknown : Path.GetFileNameWithoutExtension(assetPath);

                    var issue = new ProjectIssue(null, description, IssueCategory.BuildFile, new Location(assetPath));
                    issue.SetCustomProperties(new object[(int)BuildReportFileProperty.Num]
                    {
                        assetImporter != null ? assetImporter.GetType().FullName : k_Unknown,
                        content.type,
                        content.packedSize,
                        packedAsset.shortPath
                    });
                    onIssueFound(issue);
                }
            }
        }

        void NewMetaData(string key, object value, Action<ProjectIssue> onIssueFound)
        {
            onIssueFound(new ProjectIssue(k_Descriptor, key, IssueCategory.BuildSummary, new object[(int)BuildReportMetaData.Num] {value}));
        }

#endif
    }
}
