using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class BuildReportView : AnalysisView
    {
        struct GroupStats
        {
            public string assetGroup;
            public int count;
            public long size;
        }

        GroupStats[] m_GroupStats;
        List<ProjectIssue> m_MetaData = new List<ProjectIssue>();

        public BuildReportView(ViewManager viewManager) :
            base(viewManager)
        {
        }

        public override void AddIssues(IEnumerable<ProjectIssue> allIssues)
        {
            base.AddIssues(allIssues);
            if (m_Desc.category == IssueCategory.BuildFile)
            {
                var list = m_Issues.GroupBy(i => i.GetCustomProperty(BuildReportFileProperty.ImporterType)).Select(g => new GroupStats
                {
                    assetGroup = g.Key,
                    count = g.Count(),
                    size = g.Sum(s => s.GetCustomPropertyAsLong(BuildReportFileProperty.Size))
                }).ToList();
                list.Sort((a, b) => b.size.CompareTo(a.size));
                m_GroupStats = list.ToArray();
            }

            m_MetaData.AddRange(allIssues.Where(i => i.category == IssueCategory.BuildSummary));
        }

        public override void Clear()
        {
            base.Clear();

            m_GroupStats = null;
            m_MetaData.Clear();
        }

        protected override void DrawInfo()
        {
            if (m_Issues.Any(i => i.category == IssueCategory.BuildSummary))
            {
                EditorGUILayout.LabelField("Build Report is not available. Please build your project and try again.");
                return;
            }

            EditorGUILayout.BeginVertical();
            foreach (var issue in m_MetaData)
            {
                DrawKeyValue(issue.description, issue.GetCustomProperty(BuildReportMetaData.Value));
            }
            EditorGUILayout.EndVertical();

            if (m_Desc.category == IssueCategory.BuildFile && m_GroupStats != null && m_GroupStats.Length > 0)
            {
                EditorGUILayout.Space();

                var width = 180;
                var dataSize = m_GroupStats.Sum(g => g.size);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Size of Data (Uncompressed)", SharedStyles.Label, GUILayout.Width(width));
                EditorGUILayout.LabelField(Formatting.FormatSize((ulong)dataSize), SharedStyles.Label);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField("Size By Asset Importer", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                EditorGUILayout.BeginVertical();

                var barColor = new Color(0.0f, 0.6f, 0.6f);
                var maxGroupSize = (float)m_GroupStats.Max(g => g.size);
                foreach (var group in m_GroupStats)
                {
                    var groupSize = group.size;
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField(string.Format("{0} ({1}):", group.assetGroup, group.count), SharedStyles.Label, GUILayout.Width(260));

                    var rect = EditorGUILayout.GetControlRect(GUILayout.Width(width));
                    if (m_2D.DrawStart(rect))
                    {
                        m_2D.DrawFilledBox(0, 1, Math.Max(1, rect.width * groupSize / maxGroupSize), rect.height - 1, barColor);
                        m_2D.DrawEnd();
                    }

                    EditorGUILayout.LabelField(string.Format("{0} / {1:0.0}%", Formatting.FormatSize((ulong)group.size), 100 * groupSize / (float)dataSize), SharedStyles.Label);
                    EditorGUILayout.Space();
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();

                EditorGUI.indentLevel--;
            }
        }

        public override void DrawDetails(ProjectIssue[] selectedIssues)
        {
            EditorGUILayout.BeginVertical();

            if (selectedIssues.Length == 0)
                GUILayout.TextArea(k_NoSelectionText, SharedStyles.TextAreaWithDynamicSize, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            else if (selectedIssues.Length > 1)
                GUILayout.TextArea(k_MultipleSelectionText, SharedStyles.TextAreaWithDynamicSize, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            else // if (selectedDescriptors.Length == 1)
            {
                var description = selectedIssues[0].description;
                if (m_Desc.category == IssueCategory.BuildStep)
                {
                    description = selectedIssues[0].GetCustomProperty(BuildReportStepProperty.Message);
                }
                else if (m_Desc.category == IssueCategory.BuildFile)
                {
                    description = selectedIssues[0].relativePath;
                }

                GUILayout.TextArea(description, SharedStyles.TextAreaWithDynamicSize, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            }

            EditorGUILayout.EndVertical();
        }

        void DrawKeyValue(string key, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(string.Format("{0}:", key), SharedStyles.Label, GUILayout.ExpandWidth(false));
            EditorGUILayout.LabelField(value, SharedStyles.Label, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
        }
    }
}
