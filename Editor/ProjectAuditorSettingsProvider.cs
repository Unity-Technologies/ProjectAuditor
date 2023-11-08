using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    // Register a SettingsProvider using IMGUI for the drawing framework:
    static class ProjectAuditorSettingsIMGUIRegister
    {
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Project Settings window.
            var provider = new SettingsProvider("Project/ProjectAuditor", SettingsScope.Project)
            {
                // By default the last token of the path is used as display name if no label is provided.
                label = "Project Auditor",
                // Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
                guiHandler = (searchContext) =>
                {
                    var settings = ProjectAuditorSettings.instance.GetSerializedObject();
                    EditorGUILayout.PropertyField(settings.FindProperty("Rules"), new GUIContent("Rules"));
                    EditorGUILayout.PropertyField(settings.FindProperty("DiagnosticParams"), new GUIContent("Diagnostic Parameters"));
                    settings.ApplyModifiedPropertiesWithoutUndo();
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "Rules", "Diagnostic Parameters" })
            };

            return provider;
        }
    }
}

