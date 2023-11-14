using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Project-specific settings
    /// </summary>
    [FilePath("ProjectSettings/ProjectAuditorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class ProjectAuditorSettings : ScriptableSingleton<ProjectAuditorSettings>
    {
        public SeverityRules Rules;
        public DiagnosticParams DiagnosticParams;

        public ProjectAuditorSettings()
        {
            Rules = new SeverityRules();
            DiagnosticParams = new DiagnosticParams();
        }

        private void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave & ~HideFlags.NotEditable;
        }

        void OnDisable()
        {
            Save();
        }

        /// <summary>
        /// Save the Project Auditor Settings file.
        /// </summary>
        public void Save()
        {
            DiagnosticParams.OnBeforeSerialize();
            Save(true);
        }

        internal SerializedObject GetSerializedObject()
        {
            return new SerializedObject(this);
        }
    }
}
