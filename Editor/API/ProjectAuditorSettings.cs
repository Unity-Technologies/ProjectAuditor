using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Project-specific settings
    /// </summary>
#if UNITY_2020_1_OR_NEWER
    // FilePathAttribute.Location was private before 2020.1, meaning FilePathAttribute didn't work.
    // Everything else works fine if we disable this in old versions of Unity, except that Rules and DiagnosticParams
    // unfortunately don't persist between sessions.
    [FilePath("ProjectSettings/ProjectAuditorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
#endif
    public class ProjectAuditorSettings : ScriptableSingleton<ProjectAuditorSettings>
    {
        // <summary>
        /// The SeverityRules object which defines which issues should be ignored or given increased severity when viewing reports.
        /// </summary>
        public SeverityRules Rules;

        /// <summary>
        /// The DiagnosticParams object which defines the customizable thresholds for reporting certain diagnostics.
        /// </summary>
        public DiagnosticParams DiagnosticParams;

        /// <summary>
        /// Default constructor.
        /// </summary>
        internal ProjectAuditorSettings()
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
