using System;
using System.Collections.Generic;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Auditor interface. Any class implementing the IAuditor interface will be instantiated by ProjectAuditor and used to audit the project
    /// </summary>
    public interface IAuditor
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
        /// <param name="progressBar"> Progress bar, if applicable </param>
        void Audit(Action<ProjectIssue> onIssueFound, Action onComplete, IProgressBar progressBar = null);
    }
}
