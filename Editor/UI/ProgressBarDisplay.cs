using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    class ProgressBarDisplay
    {
        private int m_Total;
        private int m_Current;
        private string m_Title;
        private string m_Description;

        public ProgressBarDisplay(string title, string description, int total)
        {
            m_Current = 0;
            m_Total = total;

            m_Title = title;
            m_Description = description;

            EditorUtility.DisplayProgressBar(m_Title, m_Description, m_Current);
        }

        public void AdvanceProgressBar(string description = "")
        {
            if (!string.IsNullOrEmpty(description))
                m_Description = description;
            m_Current++;
            int currentFrame = Mathf.Clamp(0, m_Current, m_Total);
            float progress = m_Total > 0 ? (float)currentFrame / m_Total : 0f;
            EditorUtility.DisplayProgressBar(m_Title, description, progress);
        }

        public void ClearProgressBar()
        {
            EditorUtility.ClearProgressBar();
        }
    }
}