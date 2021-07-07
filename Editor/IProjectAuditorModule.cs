using System;
using System.Collections.Generic;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Project Auditor module interface. Any class implementing the IProjectAuditorModule interface will be instantiated by ProjectAuditor and used to audit the project
    /// </summary>
    public interface IProjectAuditorModule
    {
        IEnumerable<ProblemDescriptor> GetDescriptors();

        IEnumerable<IssueLayout> GetLayouts();

        void Initialize(ProjectAuditorConfig config);

        bool IsSupported();

        void RegisterDescriptor(ProblemDescriptor descriptor);

        /// <summary>
        /// This method audits the Unity project specific IssueCategory issues.
        /// </summary>
        /// <param name="onIssueFound"> Action called whenever a new issue is found </param>
        /// <param name="onComplete"> Action called when the analysis completes </param>
        /// <param name="progress"> Progress bar, if applicable </param>
        void Audit(Action<ProjectIssue> onIssueFound, Action onComplete = null, IProgress progress = null);
    }
}
