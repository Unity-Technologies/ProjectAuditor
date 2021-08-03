using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.Auditors;
using Unity.ProjectAuditor.Editor.Utils;
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
        List<ProjectIssue> m_MetaData = new List<ProjectIssue>();

        public BuildReportView(ViewManager viewManager) :
            base(viewManager)
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

            m_MetaData.AddRange(allIssues.Where(i => i.category == IssueCategory.BuildSummary));
        }

        public override void Clear()
        {
            base.Clear();

            m_GroupStats = null;
            m_MetaData.Clear();
        }

        protected override void OnDrawInfo()
        {
            EditorGUILayout.BeginVertical();
            foreach (var issue in m_MetaData)
            {
                DrawKeyValue(issue.description, issue.GetCustomProperty(BuildReportMetaData.Value));
            }
            EditorGUILayout.EndVertical();

            if (m_Desc.category == IssueCategory.BuildFile && m_GroupStats.Length > 0)
            {
                EditorGUILayout.Space();

                var width = 180;
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

                    EditorGUILayout.LabelField(string.Format("{0} ({1}):", group.assetGroup, group.count), GUILayout.Width(200));

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

        void DrawKeyValue(string key, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(string.Format("{0}:", key), GUILayout.ExpandWidth(false));
            EditorGUILayout.LabelField(value,  GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
        }
    }
}
