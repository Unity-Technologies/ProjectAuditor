using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Diagnostic;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Core
{
    // stephenm TODO: Make this public (and move it to API) for extensibility. Phase 2.
    /// <summary>
    /// Project Auditor module base class. Any class derived from Module will be instantiated by ProjectAuditor and used to audit the project
    /// </summary>
    internal abstract class Module
    {
        protected HashSet<DescriptorID> m_Ids;

        public abstract string Name
        {
            get;
        }

        public IssueCategory[] Categories
        {
            get { return SupportedLayouts.Select(l => l.category).ToArray(); }
        }

        public virtual bool IsEnabledByDefault => true;

        public virtual bool IsSupported => true;

        public IReadOnlyCollection<DescriptorID> SupportedDescriptorIds => m_Ids != null ? m_Ids.ToArray() : Array.Empty<DescriptorID>();

        public abstract IReadOnlyCollection<IssueLayout> SupportedLayouts
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
            m_Ids = new HashSet<DescriptorID>();
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

            if (!m_Ids.Add(descriptor.id))
                throw new Exception("Duplicate descriptor with Id: " + descriptor.id);
        }

        public bool SupportsDescriptor(DescriptorID id)
        {
            return m_Ids.Contains(id);
        }

        /// <summary>
        /// This method audits the Unity project specific IssueCategory issues.
        /// </summary>
        /// <param name="analysisParams"> Project audit parameters  </param>
        /// <param name="progress"> Progress bar, if applicable </param>
        public abstract void Audit(AnalysisParams analysisParams, IProgress progress = null);
    }
}
