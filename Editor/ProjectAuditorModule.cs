using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        public virtual bool IsEnabledByDefault()
        {
            return true;
        }

        public virtual bool IsSupported()
        {
            return true;
        }

        public virtual void RegisterDescriptor(ProblemDescriptor descriptor)
        {}

        /// <summary>
        /// Helper method for synchronous Module Audit
        /// </summary>
        /// <param name="progress"> Progress bar, if applicable </param>
        public IReadOnlyCollection<ProjectIssue> Audit(IProgress progress = null)
        {
            return Audit(new ProjectAuditorParams(), progress);
        }

        /// <summary>
        /// Helper method for synchronous Module Audit
        /// </summary>
        /// <param name="projectAuditorParams"> Parameters </param>
        /// <param name="progress"> Progress bar, if applicable </param>
        public IReadOnlyCollection<ProjectIssue> Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            var task = AuditAsync(projectAuditorParams, progress);

            task.Wait();

            return task.Result;
        }

        /// <summary>
        /// Asynchronous Module Audit. Each module must implement a AuditAsync method.
        /// </summary>
        /// <param name="projectAuditorParams"> Parameters </param>
        /// <param name="progress"> Progress bar, if applicable </param>
        public abstract Task<IReadOnlyCollection<ProjectIssue>> AuditAsync(ProjectAuditorParams projectAuditorParams, IProgress progress = null);
    }
}
