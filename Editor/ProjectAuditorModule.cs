using System;
using System.Collections.Generic;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Project Auditor module base class. Any class implementing the IProjectAuditorModule interface will be instantiated by ProjectAuditor and used to audit the project
    /// </summary>
    public abstract class ProjectAuditorModule
    {
        public virtual IEnumerable<ProblemDescriptor> GetDescriptors()
        {
            yield return null;
        }

        public abstract IEnumerable<IssueLayout> GetLayouts();

        public virtual void Initialize(ProjectAuditorConfig config)
        {
        }

        public virtual bool IsSupported()
        {
            return true;
        }

        public virtual void RegisterDescriptor(ProblemDescriptor descriptor)
        {}

        /// <summary>
        /// This method audits the Unity project specific IssueCategory issues.
        /// </summary>
        /// <param name="onIssueFound"> Action called whenever a new issue is found </param>
        /// <param name="onComplete"> Action called when the analysis completes </param>
        /// <param name="progress"> Progress bar, if applicable </param>
        public abstract void Audit(Action<ProjectIssue> onIssueFound, Action onComplete = null, IProgress progress = null);
    }
}
