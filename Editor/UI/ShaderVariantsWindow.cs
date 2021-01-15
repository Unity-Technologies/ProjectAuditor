using System;
using System.IO;
using Unity.ProjectAuditor.Editor.Auditors;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class ShaderVariantsWindow : AnalysisWindow
    {
        ShadersAuditor m_ShadersAuditor;

        public void SetShadersAuditor(ShadersAuditor shadersAuditor)
        {
            m_ShadersAuditor = shadersAuditor;
        }

        void ParsePlayerLog(string logFilename)
        {
            if (string.IsNullOrEmpty(logFilename))
                return;

            m_ShadersAuditor.ParsePlayerLog(logFilename, m_Issues.ToArray(), new ProgressBarDisplay());
        }

        public override void OnGUI()
        {
            EditorGUILayout.LabelField("Drag & Drop Player.log file on this window to find compiled variants");
            EditorGUILayout.Separator();

            base.OnGUI();

            if (Event.current.type == EventType.DragExited)
            {
                HandleDragAndDrop();
            }
        }

        void HandleDragAndDrop()
        {
            var paths = DragAndDrop.paths;
            foreach (var path in paths)
            {
                if (Path.HasExtension(path) && Path.GetExtension(path).Equals(".log"))
                    ParsePlayerLog(path);
            }
        }
    }
}
