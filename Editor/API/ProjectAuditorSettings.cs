using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Project-specific settings.
    /// </summary>
    /// <remarks>
    /// The settings in this class include the global <seealso cref="Unity.ProjectAuditor.Editor.DiagnosticParams"/> and a structure containing a list of <seealso cref="Unity.ProjectAuditor.Editor.Diagnostic.Rule"/>s.
    /// These can be viewed and edited in the Settings > Project Auditor window in the Editor and are saved in ProjectSettings/ProjectAuditorSettings.asset, but
    /// they are not directly exposed to scripts in the package API.
    /// </remarks>
    [FilePath("ProjectSettings/ProjectAuditorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class ProjectAuditorSettings : ScriptableSingleton<ProjectAuditorSettings>
    {
        // The SeverityRules object which defines which issues should be ignored or given increased severity when viewing reports.
        [SerializeField] internal SeverityRules Rules;

        // The DiagnosticParams object which defines the customizable thresholds for reporting certain diagnostics.
        [SerializeField] internal DiagnosticParams DiagnosticParams;

        // Default constructor.
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
