using System;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    public class ProgressBar : IProgress
    {
        int m_Current;
        string m_Description;
        string m_Title;
        int m_Total;

        public void Advance(string description = "")
        {
            if (!string.IsNullOrEmpty(description))
                m_Description = description;
            m_Current++;
            var currentFrame = Mathf.Clamp(0, m_Current, m_Total);
            var progress = m_Total > 0 ? (float)currentFrame / m_Total : 0f;
            EditorUtility.DisplayProgressBar(m_Title, description, progress);
        }

        public void Start(string title, string description, int total)
        {
            m_Current = 0;
            m_Total = total;

            m_Title = title;
            m_Description = description;

            EditorUtility.DisplayProgressBar(m_Title, m_Description, m_Current);
        }

        public void Clear()
        {
            EditorUtility.ClearProgressBar();
        }
    }
}
