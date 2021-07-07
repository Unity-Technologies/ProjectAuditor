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
            if (m_Desc.category == IssueCategory.BuildFile)
            {
                var list = m_Issues.GroupBy(i => i.descriptor).Select(g => new GroupStats
                {
                    assetGroup = g.Key.description,
                    count = g.Count(),
                    size = g.Sum(s => s.GetCustomPropertyAsLong(BuildReportFileProperty.Size))
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
            var report = BuildReportModule.GetBuildReport();
            if (report == null)
            {
                EditorGUILayout.LabelField("Build Report summary not found");
            }
            else
            {
                if (m_Desc.category == IssueCategory.BuildStep)
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

                    var barColor = new Color(0.0f, 0.6f, 0.6f);
                    var maxGroupSize = (float)m_GroupStats.Max(g => g.size);
                    foreach (var group in m_GroupStats)
                    {
                        var groupSize = group.size;
                        EditorGUILayout.BeginHorizontal();

                        EditorGUILayout.LabelField(string.Format("{0}:", group.assetGroup), GUILayout.Width(200));

                        var rect = EditorGUILayout.GetControlRect(GUILayout.Width(width));
                        if (m_2D.DrawStart(rect))
                        {
                            m_2D.DrawFilledBox(0, 1, Math.Max(1, rect.width * groupSize / maxGroupSize), rect.height - 1, barColor);
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
