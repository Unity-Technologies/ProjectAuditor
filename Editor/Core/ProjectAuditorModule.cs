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
        protected HashSet<Descriptor> m_Descriptors;

        internal abstract string name
        {
            get;
        }

        internal IssueCategory[] categories
        {
            get { return supportedLayouts.Select(l => l.category).ToArray(); }
        }

        internal virtual bool isEnabledByDefault => true;

        internal virtual bool isSupported => true;

        internal IReadOnlyCollection<Descriptor> supportedDescriptors => m_Descriptors != null ? m_Descriptors.ToArray() : Array.Empty<Descriptor>();

        internal abstract IReadOnlyCollection<IssueLayout> supportedLayouts
        {
            get;
        }

        internal virtual void Initialize(ProjectAuditorConfig config)
        {
            m_Descriptors = new HashSet<Descriptor>();
        }

        internal void RegisterDescriptor(Descriptor descriptor)
        {
            if (!m_Descriptors.Add(descriptor))
                throw new Exception("Duplicate descriptor with id: " + descriptor.id);
        }

        /// <summary>
        /// This method audits the Unity project specific IssueCategory issues.
        /// </summary>
        /// <param name="projectAuditorParams"> Project audit parameters  </param>
        /// <param name="progress"> Progress bar, if applicable </param>
        internal abstract void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null);
    }
}
