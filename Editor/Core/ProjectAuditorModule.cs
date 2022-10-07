using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Diagnostic;

namespace Unity.ProjectAuditor.Editor.Core
{
    /// <summary>
    /// Project Auditor module base class. Any class derived from ProjectAuditorModule will be instantiated by ProjectAuditor and used to audit the project
    /// </summary>
    public abstract class ProjectAuditorModule
    {
        public abstract string name
        {
            get;
        }

        public IssueCategory[] categories
        {
            get { return supportedLayouts.Select(l => l.category).ToArray(); }
        }

        public virtual bool isEnabledByDefault => true;

        public virtual bool isSupported => true;

        public virtual IReadOnlyCollection<Descriptor> supportedDescriptors => Array.Empty<Descriptor>();

        public abstract IReadOnlyCollection<IssueLayout> supportedLayouts
        {
            get;
        }

        public virtual void Initialize(ProjectAuditorConfig config)
        {
        }

        public virtual void RegisterDescriptor(Descriptor descriptor)
        {}

        /// <summary>
        /// This method audits the Unity project specific IssueCategory issues.
        /// </summary>
        /// <param name="projectAuditorParams"> Project audit parameters  </param>
        /// <param name="progress"> Progress bar, if applicable </param>
        public abstract void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null);
    }
}
