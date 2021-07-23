#if UNITY_2019_4_OR_NEWER
    #define BUILD_REPORT_API_SUPPORTED
#endif

using System;
using System.Collections.Generic;
using System.IO;
using Packages.Editor.Utils;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Auditors
{
    public enum BuildReportFileProperty
    {
        Size = 0,
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

    class LastBuildReportProvider : IBuildReportProvider, IPostprocessBuildWithReport
    {
        const string k_BuildReportDir = "Assets/BuildReports";
        const string k_LastBuildReportPath = "Library/LastBuild.buildreport";

        static BuildReport s_BuildReport;

        public BuildReport GetBuildReport()
        {
            if (s_BuildReport != null)
                return s_BuildReport;

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
            s_BuildReport = AssetDatabase.LoadAssetAtPath<BuildReport>(assetPath);
            return s_BuildReport;
        }

        public int callbackOrder { get; }
        public void OnPostprocessBuild(BuildReport report)
        {
            s_BuildReport = report;
        }
    }

    class BuildReportModule : IProjectAuditorModule
    {
        static readonly ProblemDescriptor k_InfoDescriptor = new ProblemDescriptor
            (
            600000,
            "Build step info"
            )
        {
            severity = Rule.Severity.Info
        };

        static readonly ProblemDescriptor k_WarnDescriptor = new ProblemDescriptor
            (
            600001,
            "Build step warning"
            )
        {
            severity = Rule.Severity.Warning
        };

        static readonly ProblemDescriptor k_ErrorDescriptor = new ProblemDescriptor
            (
            600002,
            "Build step error"
            )
        {
            severity = Rule.Severity.Error
        };

        static readonly ProblemDescriptor k_AssetDescriptor = new ProblemDescriptor
            (
            600003,
            "Asset",
            Area.BuildSize
            );

        static readonly ProblemDescriptor k_ByteDataDescriptor = new ProblemDescriptor
            (
            600004,
            "Byte data",
            Area.BuildSize
            );

        static readonly ProblemDescriptor k_FontDescriptor = new ProblemDescriptor
            (
            600005,
            "Font",
            Area.BuildSize
            );

        static readonly ProblemDescriptor k_MaterialDescriptor = new ProblemDescriptor
            (
            600006,
            "Material",
            Area.BuildSize
            );

        static readonly ProblemDescriptor k_ModelDescriptor = new ProblemDescriptor
            (
            600007,
            "Model",
            Area.BuildSize
            );

        static readonly ProblemDescriptor k_PrefabDescriptor = new ProblemDescriptor
            (
            600008,
            "Prefab",
            Area.BuildSize
            );

        static readonly ProblemDescriptor k_ShaderDescriptor = new ProblemDescriptor
            (
            600009,
            "Shader",
            Area.BuildSize
            );

        static readonly ProblemDescriptor k_TextureDescriptor = new ProblemDescriptor
            (
            600010,
            "Texture",
            Area.BuildSize
            );

        static readonly ProblemDescriptor k_OtherTypeDescriptor = new ProblemDescriptor
            (
            600011,
            "Other Type",
            Area.BuildSize
            );

#pragma warning disable 0414
        readonly Dictionary<string, ProblemDescriptor> m_DescriptorByExtension = new Dictionary<string, ProblemDescriptor>()
        {
            { ".asset", k_AssetDescriptor },
            { ".compute", k_ShaderDescriptor },
            { ".shader", k_ShaderDescriptor },
            { ".png", k_TextureDescriptor },
            { ".tga", k_TextureDescriptor },
            { ".exr", k_TextureDescriptor },
            { ".mat", k_MaterialDescriptor },
            { ".fbx", k_ModelDescriptor },
            { ".ttf", k_FontDescriptor },
            { ".bytes", k_ByteDataDescriptor },
            { ".prefab", k_PrefabDescriptor },
        };
#pragma warning restore 0414

        static readonly IssueLayout k_FileLayout = new IssueLayout
        {
            category = IssueCategory.BuildFile,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Source Asset"},
                new PropertyDefinition { type = PropertyType.FileType, name = "Type"},
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

        public IEnumerable<ProblemDescriptor> GetDescriptors()
        {
            yield return k_InfoDescriptor;
            yield return k_WarnDescriptor;
            yield return k_ErrorDescriptor;

            yield return k_AssetDescriptor;
            yield return k_ByteDataDescriptor;
            yield return k_FontDescriptor;
            yield return k_MaterialDescriptor;
            yield return k_ModelDescriptor;
            yield return k_PrefabDescriptor;
            yield return k_ShaderDescriptor;
            yield return k_TextureDescriptor;
            yield return k_OtherTypeDescriptor;
        }

        public IEnumerable<IssueLayout> GetLayouts()
        {
            yield return k_FileLayout;
            yield return k_StepLayout;
        }

        public void Initialize(ProjectAuditorConfig config)
        {
        }

        public bool IsSupported()
        {
#if BUILD_REPORT_API_SUPPORTED
            return true;
#else
            return false;
#endif
        }

        public void RegisterDescriptor(ProblemDescriptor descriptor)
        {
        }

        public void Audit(Action<ProjectIssue> onIssueFound, Action onComplete = null, IProgress progress = null)
        {
#if BUILD_REPORT_API_SUPPORTED
            var buildReport = BuildReportProvider.GetBuildReport();
            if (buildReport != null)
            {
                AnalyzeBuildSteps(onIssueFound, buildReport);
                AnalyzePackedAssets(onIssueFound, buildReport);
            }
#endif
            if (onComplete != null)
                onComplete();
        }

#if BUILD_REPORT_API_SUPPORTED
        void AnalyzeBuildSteps(Action<ProjectIssue> onIssueFound, BuildReport buildReport)
        {
            foreach (var step in buildReport.steps)
            {
                var depth = step.depth;
                onIssueFound(new ProjectIssue(k_InfoDescriptor, step.name, IssueCategory.BuildStep, new object[(int)BuildReportStepProperty.Num]
                {
                    Formatting.FormatTime(step.duration)
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
                var dict = new Dictionary<GUID, List<PackedAssetInfo>>();
                foreach (var content in packedAsset.contents)
                {
                    var assetPath = content.sourceAssetPath;
                    if (!Path.HasExtension(assetPath))
                        continue;

                    if (Path.GetExtension(assetPath).Equals(".cs"))
                        continue;

                    if (!dict.ContainsKey(content.sourceAssetGUID))
                    {
                        dict.Add(content.sourceAssetGUID, new List<PackedAssetInfo>());
                    }

                    dict[content.sourceAssetGUID].Add(content);
                }

                foreach (var entry in dict)
                {
                    var content = entry.Value[0]; // sourceAssets are the same for all entries
                    var assetPath = content.sourceAssetPath;

                    ulong sum = 0;
                    foreach (var v in entry.Value)
                    {
                        sum += v.packedSize;
                    }

                    var assetName = Path.GetFileNameWithoutExtension(assetPath);
                    string description;
                    if (entry.Value.Count > 1)
                        description = string.Format("{0} ({1})", assetName, entry.Value.Count);
                    else
                        description = assetName;

                    var descriptor = k_OtherTypeDescriptor;
                    var ext = Path.GetExtension(assetPath);
                    if (m_DescriptorByExtension.ContainsKey(ext))
                        descriptor = m_DescriptorByExtension[ext];

                    var issue = new ProjectIssue(descriptor, description, IssueCategory.BuildFile, new Location(assetPath));
                    issue.SetCustomProperties(new object[(int)BuildReportFileProperty.Num]
                    {
                        sum,
                        packedAsset.shortPath
                    });
                    onIssueFound(issue);
                }
            }
        }

#endif
    }
}
