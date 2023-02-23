using System;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    public static class ChartUtil
    {
        public struct Element
        {
            public string Label;
            public float Value;
            public Color Color;

            public Element(string label, float value, Color color)
            {
                Label = label;
                Value = value;
                Color = color;
            }
        }

        const int k_NumberLabelWidth = 60;
        const int k_RowSize = 22;

        static readonly Draw2D s_2D = new Draw2D("Unlit/ProjectAuditor");

        static readonly GUIStyle s_Row = new GUIStyle(GUIStyle.none)
        {
            normal = {background = Utility.MakeColorTexture(new Color(0.5f, 0.5f, 0.5f, 0.0f))},
            fixedHeight = k_RowSize
        };

        public static void DrawHorizontalStackedBar(string title, Element[] inValues, string labelFormat = "{0}", string numberFormat = "#")
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField(title, SharedStyles.BoldLabel);

            float totalValue = 0;
            for (int i = 0; i < inValues.Length; ++i)
                totalValue += inValues[i].Value;

            var rect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
            int x = 0;

            for (int i = 0; i < inValues.Length; ++i)
            {
                var value = inValues[i].Value;

                if (value > 0 && s_2D.DrawStart(rect))
                {
                    var barWidth = (int)Math.Max(1f, rect.width * value / totalValue);
                    s_2D.DrawFilledBox(x, 1,  barWidth - 2, rect.height - 1, inValues[i].Color);
                    s_2D.DrawEnd();

                    x += barWidth;
                }
            }

            EditorGUILayout.Space(3);

            int firstColumnNum = (inValues.Length + 1) / 2;
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    for (int i = 0; i < firstColumnNum; ++i)
                    {
                        DrawLegendItem(inValues[i].Label, inValues[i].Value, inValues[i].Color, labelFormat,
                            numberFormat);
                    }

                    DrawLine();
                }

                GUILayout.Space(10);

                using (new EditorGUILayout.VerticalScope())
                {
                    for (int i = firstColumnNum; i < inValues.Length; ++i)
                    {
                        DrawLegendItem(inValues[i].Label, inValues[i].Value, inValues[i].Color, labelFormat,
                            numberFormat);
                    }

                    DrawLine();
                }
            }

            EditorGUILayout.EndVertical();
        }

        static void DrawLegendItem(string text, float count, Color color, string labelFormat = "{0}", string numberFormat = "#")
        {
            DrawLine();

            EditorGUILayout.BeginHorizontal(s_Row, GUILayout.Height(10));

            var rect = EditorGUILayout.GetControlRect(GUILayout.Width(10), GUILayout.Height(20));
            if (s_2D.DrawStart(rect))
            {
                s_2D.DrawFilledBox(0, 4, 10, 10, color);
                s_2D.DrawEnd();
            }

            EditorGUILayout.LabelField(text, SharedStyles.BoldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(String.Format(labelFormat, count.ToString(numberFormat)), SharedStyles.BoldLabel, GUILayout.Width(k_NumberLabelWidth));

            EditorGUILayout.EndHorizontal();
        }

        static void DrawLine()
        {
            var rect = EditorGUILayout.GetControlRect(GUILayout.Height(1));
            var color = new Color(0.3f, 0.3f, 0.3f);

            if (s_2D.DrawStart(rect))
            {
                s_2D.DrawLine(0, 0, rect.width, 0, color);
                s_2D.DrawEnd();
            }
        }
    }
}
