using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Diagnostic;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Core
{
    /// <summary>
    /// Project Auditor module base class. Any class derived from Module will be instantiated by ProjectAuditor and used to audit the project
    /// </summary>
    internal abstract class Module
    {
        protected HashSet<DescriptorID> m_IDs;

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

        public IReadOnlyCollection<DescriptorID> supportedDescriptorIDs => m_IDs != null ? m_IDs.ToArray() : Array.Empty<DescriptorID>();

        public abstract IReadOnlyCollection<IssueLayout> supportedLayouts
        {
            get;
        }

        public static string[] GetAssetPaths()
        {
            return AssetDatabase.GetAllAssetPaths();
        }

        public static string[] GetAssetPathsByFilter(string filter)
        {
            return AssetDatabase.FindAssets(filter).Select(AssetDatabase.GUIDToAssetPath).ToArray();
        }

        public virtual void Initialize()
        {
            m_IDs = new HashSet<DescriptorID>();
        }

        public virtual void RegisterParameters(DiagnosticParams diagnosticParams)
        {
        }

        public void RegisterDescriptor(Descriptor descriptor)
        {
            // Don't register descriptors that aren't applicable to this Unity version, or to platforms that aren't supported
            if (!descriptor.IsPlatformSupported())
                return;

            if (!descriptor.IsVersionCompatible())
                return;

            DescriptorLibrary.RegisterDescriptor(descriptor.id, descriptor);

            if (!m_IDs.Add(descriptor.id))
                throw new Exception("Duplicate descriptor with id: " + descriptor.id);
        }

        public bool SupportsDescriptor(DescriptorID id)
        {
            return m_IDs.Contains(id);
        }

        /// <summary>
        /// This method audits the Unity project specific IssueCategory issues.
        /// </summary>
        /// <param name="analysisParams"> Project audit parameters  </param>
        /// <param name="progress"> Progress bar, if applicable </param>
        public abstract void Audit(AnalysisParams analysisParams, IProgress progress = null);
    }
}
