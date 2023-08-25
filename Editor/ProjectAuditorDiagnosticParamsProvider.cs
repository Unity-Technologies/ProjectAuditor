using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// A built-in provider to manage ProjectAuditorDiagnosticParams default and user-generated assets for customization.
    /// </summary>
    public class ProjectAuditorDiagnosticParamsProvider : IProjectAuditorDiagnosticParamsProvider
    {
        HashSet<ProjectAuditorDiagnosticParams> m_Assets = new HashSet<ProjectAuditorDiagnosticParams>();
        ProjectAuditorDiagnosticParams m_CurrentParams;
        ProjectAuditorDiagnosticParams m_DefaultParams;

        /// <summary>
        /// Initializes default ProjectAuditorDiagnosticParams objects, and loads any others that are found in the project
        /// </summary>
        public void Initialize()
        {
            RefreshAssets();
        }

        private ProjectAuditorDiagnosticParams GetOrCreateDefaultParams()
        {
            if (m_DefaultParams == null)
            {
                m_DefaultParams = ScriptableObject.CreateInstance<ProjectAuditorDiagnosticParams>();
                m_DefaultParams.name = "Default";
            }

            return m_DefaultParams;
        }

        /// <summary>
        /// Loads (or reloads) all ProjectAuditorDiagnosticParams objects in the project
        /// </summary>
        internal void RefreshAssets()
        {
            m_CurrentParams = GetOrCreateDefaultParams();

            var allSettingsAssets = AssetDatabase.FindAssets("t:ProjectAuditorSettings, a:assets");

            m_Assets.Clear();
            foreach (var assetGuid in allSettingsAssets)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                var settingsAsset = AssetDatabase.LoadAssetAtPath<ProjectAuditorDiagnosticParams>(assetPath);
                AddSettingsFromAsset(settingsAsset);
            }

            // restore settings
            var guidAsString = UserPreferences.settingsAsset;
            var guid = new GUID(guidAsString);

            if (guidAsString.Length > 0 && !guid.Empty())
            {
                var path = AssetDatabase.GUIDToAssetPath(guidAsString);
                var settings = AssetDatabase.LoadAssetAtPath<ProjectAuditorDiagnosticParams>(path);

                if (settings != null)
                    m_CurrentParams = settings;
                else
                    SelectCurrentParams(GetOrCreateDefaultParams());
            }
        }

        /// <summary>
        /// Gets <see cref="ProjectAuditorDiagnosticParams"/> that were last selected, initially the default settings.
        /// </summary>
        /// <returns>A ProjectAuditorSettings</returns>
        public ProjectAuditorDiagnosticParams GetCurrentParams()
        {
            if (m_CurrentParams == null)
            {
                m_CurrentParams = GetOrCreateDefaultParams();
                RefreshAssets();
            }

            return m_CurrentParams;
        }

        /// <summary>
        /// Gets all ProjectAuditorSettings objects
        /// </summary>
        /// <returns>An IEnumerable of all the ProjectAuditorSettings objects found in the project.</returns>
        public IEnumerable<ProjectAuditorDiagnosticParams> GetParams()
        {
            RefreshAssets();

            yield return GetOrCreateDefaultParams();

            foreach (var settingsAsset in m_Assets)
            {
                yield return settingsAsset;
            }
        }

        /// <summary>
        /// Adds a settings asset that was stored in the past or just created by the user.
        /// </summary>
        /// <param name="diagnosticParamsAsset">A ScriptableObject asset of type <see cref="ProjectAuditorDiagnosticParams"/> to be used as the settings to tweak analyzer values/limits.</param>
        internal void AddSettingsFromAsset(ProjectAuditorDiagnosticParams diagnosticParamsAsset)
        {
            if (!m_Assets.Contains(diagnosticParamsAsset))
            {
                m_Assets.Add(diagnosticParamsAsset);
            }
        }

        /// <summary>
        /// Selects settings as the current settings. This may be the default settings or an asset with settings.
        /// </summary>
        /// <param name="diagnosticParams">A ScriptableObject asset of type <see cref="ProjectAuditorDiagnosticParams"/> to be used as the settings to tweak analyzer values/limits.</param>
        public void SelectCurrentParams(ProjectAuditorDiagnosticParams diagnosticParams)
        {
            m_CurrentParams = diagnosticParams;

            var path = AssetDatabase.GetAssetPath(diagnosticParams);
            var guidAsString = AssetDatabase.AssetPathToGUID(path);

            UserPreferences.settingsAsset = guidAsString;
        }
    }
}
