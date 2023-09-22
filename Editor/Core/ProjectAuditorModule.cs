using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Diagnostic;

namespace Unity.ProjectAuditor.Editor.Core
{
    /// <summary>
    /// Project Auditor module base class. Any class derived from ProjectAuditorModule will be instantiated by ProjectAuditor and used to audit the project
    /// </summary>
    internal abstract class ProjectAuditorModule
    {
        protected HashSet<string> m_IDs;

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

        public IReadOnlyCollection<string> supportedDescriptorIDs => m_IDs != null ? m_IDs.ToArray() : Array.Empty<string>();

        public abstract IReadOnlyCollection<IssueLayout> supportedLayouts
        {
            get;
        }

        public virtual void Initialize(ProjectAuditorConfig config)
        {
            m_IDs = new HashSet<string>();
        }

        public void RegisterDescriptor(Descriptor descriptor)
        {
            DescriptorLibrary.RegisterDescriptor(descriptor.id, descriptor);

            if (!m_IDs.Add(descriptor.id))
                throw new Exception("Duplicate descriptor with id: " + descriptor.id);
        }

        /// <summary>
        /// This method audits the Unity project specific IssueCategory issues.
        /// </summary>
        /// <param name="projectAuditorParams"> Project audit parameters  </param>
        /// <param name="progress"> Progress bar, if applicable </param>
        public abstract void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null);
    }
}
