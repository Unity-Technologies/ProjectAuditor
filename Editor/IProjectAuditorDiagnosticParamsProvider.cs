using System.Collections.Generic;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Provides methods for a way to gain access to a ProjectAuditorDiagnosticParams object
    /// </summary>
    internal interface IProjectAuditorDiagnosticParamsProvider
    {
        /// <summary>
        /// Initializes default ProjectAuditorDiagnosticParams
        /// </summary>
        void Initialize();

        /// <summary>
        /// Gets all ProjectAuditorDiagnosticParams objects
        /// </summary>
        /// <returns>All the param objects found in the project</returns>
        IEnumerable<ProjectAuditorDiagnosticParams> GetParams();

        /// <summary>
        /// Gets <see cref="ProjectAuditorDiagnosticParams"/> that were last selected, initially the default settings.
        /// </summary>
        /// <returns>A ProjectAuditorDiagnosticParams</returns>
        ProjectAuditorDiagnosticParams GetCurrentParams();

        /// <summary>
        /// Sets the current ProjectAuditorDiagnosticParams object
        /// </summary>
        /// <param name="diagnosticParams">The ProjectAuditorDiagnosticParams object to be considered the current one.</param>
        void SelectCurrentParams(ProjectAuditorDiagnosticParams diagnosticParams);
    }
}
