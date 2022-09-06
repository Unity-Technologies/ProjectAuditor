using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.ProjectAuditor.Editor.Core
{
    /// <summary>
    /// Project Auditor module base class. Any class derived from ProjectAuditorModule will be instantiated by ProjectAuditor and used to audit the project
    /// </summary>
    public abstract class ProjectAuditorModule
    {
        public IssueCategory[] GetCategories()
        {
            return GetLayouts().Select(l => l.category).ToArray();
        }

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
        /// This method audits the Unity project specific IssueCategory issues.
        /// </summary>
        /// <param name="projectAuditorParams"> Project audit parameters  </param>
        /// <param name="progress"> Progress bar, if applicable </param>
        public abstract void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null);
    }
}
