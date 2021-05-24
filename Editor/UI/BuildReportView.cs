using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Packages.Editor.Utils;
using Unity.ProjectAuditor.Editor.Auditors;
using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class BuildReportView : AnalysisView
    {
        readonly Draw2D m_2D;

        private const string k_StringAssets = "Assets";
        private const string k_StringByteData = "Byte data";
        private const string k_StringFonts = "Fonts";
        private const string k_StringMaterials = "Materials";
        private const string k_StringModels = "Models";
        private const string k_StringPrefabs = "Prefabs";
        private const string k_StringShaders = "Shaders";
        private const string k_StringTextures = "Textures";
        private const string k_StringOtherTypes = "Other types";

        readonly Dictionary<string, string> m_AssetTypeLabels = new Dictionary<string, string>()
        {
            { ".asset", k_StringAssets },
            { ".compute", k_StringShaders },
            { ".shader", k_StringShaders },
            { ".png", k_StringTextures },
            { ".tga", k_StringTextures },
            { ".exr", k_StringTextures },
            { ".mat", k_StringMaterials },
            { ".fbx", k_StringModels },
            { ".ttf", k_StringFonts },
            { ".bytes", k_StringByteData },
            { ".prefab", k_StringPrefabs },
        };

        struct GroupStats
        {
            public string assetGroup;
            public int count;
            public long size;
        }

        GroupStats[] m_GroupStats;

        public BuildReportView()
        {
            m_2D = new Draw2D("Unlit/ProjectAuditor");
        }

        public override void AddIssues(IEnumerable<ProjectIssue> allIssues)
        {
            base.AddIssues(allIssues);
            if (m_Desc.category == IssueCategory.BuildFiles)
            {
                //var stats = new Dictionary<string, GroupStats>();
                var list = m_Issues.GroupBy(i =>
                {
                    var ext = i.location.Extension;
                    if (m_AssetTypeLabels.ContainsKey(ext))
                        return m_AssetTypeLabels[ext];
                    return k_StringOtherTypes;
                }).Select(g => new GroupStats
                    {
                        assetGroup = g.Key,
                        count = g.Count(),
                        size = g.Sum(s => s.GetCustomPropertyAsInt(BuildReportFileProperty.Size))
                    }).ToList();
                list.Sort((a, b) => b.size.CompareTo(a.size));
                m_GroupStats = list.ToArray();
            }
        }

        public override void Clear()
        {
            base.Clear();

            m_GroupStats = null;
        }

        protected override void OnDrawInfo()
        {
            var report = BuildAuditor.GetBuildReport();
            if (report == null)
            {
                EditorGUILayout.LabelField("Build Report summary not found");
            }
            else
            {
                if (m_Desc.category == IssueCategory.BuildSteps)
                {
                    EditorGUILayout.BeginVertical();

                    EditorGUILayout.LabelField("Build Name: ", Path.GetFileNameWithoutExtension(report.summary.outputPath));
                    EditorGUILayout.LabelField("Platform: ", report.summary.platform.ToString());
                    EditorGUILayout.LabelField("Build Result: ", report.summary.result.ToString());

                    EditorGUILayout.LabelField("Started at: ", report.summary.buildStartedAt.ToString());
                    EditorGUILayout.LabelField("Ended at: ", report.summary.buildEndedAt.ToString());

                    EditorGUILayout.LabelField("Total Time: ", Formatting.FormatTime(report.summary.totalTime));
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    var width = 180;
                    EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Size of Build", GUILayout.Width(width));
                        EditorGUILayout.LabelField(Formatting.FormatSize(report.summary.totalSize));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space();

                    var dataSize = m_GroupStats.Sum(g => g.size);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Size of Data (Uncompressed)", GUILayout.Width(width));
                    EditorGUILayout.LabelField(Formatting.FormatSize((ulong)dataSize));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.LabelField("Size By Asset Group", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;

                    EditorGUILayout.BeginVertical();

                    var maxGroupSize = (float)m_GroupStats.Max(g => g.size);
                    foreach (var group in m_GroupStats)
                    {
                        var groupSize = group.size;
                        EditorGUILayout.BeginHorizontal();

                        EditorGUILayout.LabelField(string.Format("{0} ({1}):", group.assetGroup, group.count), GUILayout.Width(200));

                        var rect = EditorGUILayout.GetControlRect(GUILayout.Width(width));
                        if (m_2D.DrawStart(rect))
                        {
                            m_2D.DrawFilledBox(0, 1, Math.Max(1, rect.width * groupSize / maxGroupSize), rect.height - 1, Color.white);
                            m_2D.DrawEnd();
                        }

                        EditorGUILayout.LabelField(string.Format("{0} / {1:0.0}%", Formatting.FormatSize((ulong)group.size), 100 * groupSize / (float)dataSize));
                        EditorGUILayout.Space();
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();

                    EditorGUI.indentLevel--;
                }
            }
        }
    }
}
