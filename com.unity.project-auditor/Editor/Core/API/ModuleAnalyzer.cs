using System;
using System.Reflection;

namespace Unity.ProjectAuditor.Editor.Core
{
    /// <summary>
    /// Base class for all ModuleAnalyzers
    /// </summary>
    /// <remarks>
    /// Inheriting directly from ModuleAnalyzer will not create an Analyzer that a Module will create or run. You should
    /// inherit from one of the following classes, all of which declare Analyze() methods:
    /// * <seealso cref="AnimationModuleAnalyzer"/>
    /// * <seealso cref="AssetsModuleAnalyzer"/>
    /// * <seealso cref="AudioClipModuleAnalyzer"/>
    /// * <seealso cref="CodeModuleInstructionAnalyzer"/>
    /// * <seealso cref="MeshModuleAnalyzer"/>
    /// * <seealso cref="PackagesModuleAnalyzer"/>
    /// * <seealso cref="SettingsModuleAnalyzer"/>
    /// * <seealso cref="ShaderModuleAnalyzer"/>
    /// * <seealso cref="SpriteAtlasModuleAnalyzer"/>
    /// * <seealso cref="TextureModuleAnalyzer"/>
    /// </remarks>
    public class ModuleAnalyzer
    {
        /// <summary>
        /// Initializes the Analyzer
        /// </summary>
        /// <param name="registerDescriptor">An Action which the method can invoke to register an Issue Descriptor for later reporting</param>
        /// <remarks>
        /// Modules and their associated Analyzers are Initialized during the process of constructing the ProjectAuditor
        /// object. The primary purpose of the Initialize method is to register Descriptors for any Issues which the
        /// Analyzer can add to the report. Descriptors must be registered before they can be used to create Issues.
        /// However, other initialization is allowed within this method if required - perhaps constructing and/or caching
        /// data structures to optimize the Analyze() methods, which may be called many times during analysis.
        /// </remarks>
        public virtual void Initialize(Action<Descriptor> registerDescriptor)
        {
        }

        internal void CacheParameters(DiagnosticParams diagnosticParams)
        {
            foreach (var field in this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var attribute = field.GetCustomAttribute<DiagnosticParameterAttribute>();
                if (attribute != null)
                {
                    field.SetValue(this, diagnosticParams.GetParameter(attribute.Name));
                }
            }
        }

        internal void RegisterParameters(DiagnosticParams diagnosticParams)
        {
            foreach (var field in this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var attribute = field.GetCustomAttribute<DiagnosticParameterAttribute>();
                if (attribute != null)
                {
                    diagnosticParams.RegisterParameter(attribute.Name, attribute.DefaultValue);
                }
            }
        }
    }
}
