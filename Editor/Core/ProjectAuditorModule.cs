using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Diagnostic;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Core
{
    /// <summary>
    /// Project Auditor module base class. Any class derived from ProjectAuditorModule will be instantiated by ProjectAuditor and used to audit the project
    /// </summary>
    internal abstract class ProjectAuditorModule
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

        public static string[] GetAssetPaths(AnalysisContext context, bool doAssetPathFilter = true)
        {
            var assets = AssetDatabase.GetAllAssetPaths();
            if (doAssetPathFilter)
            {
                return FilterAssetPathsArray(context, assets);
            }
            return assets;
        }

        public static string[] GetAssetPathsByFilter(string filter, AnalysisContext context, bool doAssetPathFilter = true)
        {
            var assetsEnumerable = AssetDatabase.FindAssets(filter).Select(AssetDatabase.GUIDToAssetPath);
            if (doAssetPathFilter && context.Params.AssetPathFilter != null)
            {
                assetsEnumerable = assetsEnumerable.Where(path => context.Params.AssetPathFilter(path));
            }
            return assetsEnumerable.ToArray();
        }

        static string[] FilterAssetPathsArray(AnalysisContext context, string[] assets)
        {
            var filter = context.Params.AssetPathFilter;
            if (filter != null)
            {
                var readIndex = 0;
                var writeIndex = 0;
                for (; readIndex < assets.Length; readIndex++)
                {
                    var asset = assets[readIndex];
                    if (filter(asset))
                    {
                        assets[writeIndex] = asset;
                        writeIndex++;
                    }
                }
                if (writeIndex == 0)
                {
                    return Array.Empty<string>();
                }
                if (writeIndex < readIndex)
                {
                    var newArray = new string[writeIndex];
                    Array.Copy(assets, newArray, writeIndex);
                    return newArray;
                }
            }
            return assets;
        }

        public virtual void Initialize()
        {
            m_IDs = new HashSet<DescriptorID>();
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
        /// <param name="projectAuditorParams"> Project audit parameters  </param>
        /// <param name="progress"> Progress bar, if applicable </param>
        public abstract void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null);
    }
}
