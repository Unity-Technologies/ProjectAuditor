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
    class BuildSizeView : BuildReportView
    {
        public override string Description => "A list of files contributing to the build size.";
        public override string InfoTitle => $@"This view shows a list of files incorporated into the last clean build of the project, and their size.";

        static readonly GUIContent SizesFoldout = new GUIContent("Sizes");

        struct GroupStats
        {
            public string assetGroup;
            public int count;
            public long size;
        }

        const int k_MaxGroupCount = 10;

        static readonly Color k_BarColor = new Color(0.0f, 0.6f, 0.6f);

        GroupStats[] m_GroupStats;

        public BuildSizeView(ViewManager viewManager) :
            base(viewManager)
        {
        }

        public override void AddIssues(IEnumerable<ReportItem> allIssues)
        {
            base.AddIssues(allIssues);

            var list = m_Issues.GroupBy(i => i.GetCustomProperty(BuildReportFileProperty.RuntimeType)).Select(g => new GroupStats
            {
                assetGroup = g.Key,
                count = g.Count(),
                size = g.Sum(s => s.GetCustomPropertyInt64(BuildReportFileProperty.Size))
            }).ToList();
            list.Sort((a, b) => b.size.CompareTo(a.size));
            m_GroupStats = list.Take(k_MaxGroupCount).ToArray();
        }

        public override void Clear()
        {
            base.Clear();
            m_GroupStats = null;
        }

        protected override void DrawAdditionalInfo()
        {
            if (m_GroupStats != null && m_GroupStats.Length > 0)
            {
                EditorGUILayout.Space();

                m_ViewStates.info2 = Utility.BoldFoldout(m_ViewStates.info2, SizesFoldout);
                if (m_ViewStates.info2)
                {
                    EditorGUI.indentLevel++;

                    var width = 180;
                    var dataSize = m_GroupStats.Sum(g => g.size);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Size of Data (Uncompressed)", SharedStyles.Label, GUILayout.Width(width));
                    EditorGUILayout.LabelField(Formatting.FormatSize((ulong)dataSize), SharedStyles.Label);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.LabelField($"Size By Runtime Type (Top {k_MaxGroupCount})", SharedStyles.BoldLabel);
                    EditorGUI.indentLevel++;

                    EditorGUILayout.BeginVertical();

                    var maxGroupSize = (float)m_GroupStats.Max(g => g.size);
                    foreach (var group in m_GroupStats)
                    {
                        var groupSize = group.size;
                        EditorGUILayout.BeginHorizontal();

                        EditorGUILayout.LabelField(string.Format("{0} ({1}):", group.assetGroup, group.count), SharedStyles.Label, GUILayout.Width(260));

                        var rect = EditorGUILayout.GetControlRect(GUILayout.Width(width));
                        if (m_2D.DrawStart(rect))
                        {
                            m_2D.DrawFilledBox(0, 1, Math.Max(1, rect.width * groupSize / maxGroupSize), rect.height - 1, k_BarColor);
                            m_2D.DrawEnd();
                        }

                        EditorGUILayout.LabelField(string.Format("{0} / {1:0.0}%", Formatting.FormatSize((ulong)group.size), 100 * groupSize / (float)dataSize), SharedStyles.Label);
                        EditorGUILayout.Space();
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();

                    EditorGUI.indentLevel--;

                    EditorGUI.indentLevel--;
                }
            }
        }

        public override string GetIssueDescription(ReportItem issue)
        {
            return issue.RelativePath;
        }
    }
}
