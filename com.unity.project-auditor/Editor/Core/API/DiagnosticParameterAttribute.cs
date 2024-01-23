using System;

namespace Unity.ProjectAuditor.Editor.Core
{
    /// <summary>
    /// Used to mark an integer field in an class that inherits from <seealso cref="ModuleAnalyzer"/> as being a Diagnostic Parameter.
    /// </summary>
    /// <remarks>
    /// Diagnostic Parameters are used to define threshold values against which to compare other values when an Analyzer
    /// is deciding whether or not something constitutes a reportable issue. Whilst Analyzers are free to use hard-coded
    /// constants as threshold values, Diagnostic Parameters allow you to change values in Settings > Project Auditor as
    /// a project's requirements evolve, or to set different values for different target platforms.
    ///
    /// Diagnostic Parameters and their default values are automatically registered in the <seealso cref="DiagnosticParams"/>
    /// object held by <seealso cref="ProjectAuditorSettings"/>, where their values can be customized if required. When
    /// <seealso cref="ProjectAuditor"/> initializes prior to running analysis, the values in the DiagnosticParams held by
    /// <seealso cref="AnalysisParams"/> are automatically cached back in their corresponding fields which can be used
    /// during analysis.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field)]
    public class DiagnosticParameterAttribute : Attribute
    {
        /// <summary>
        /// The Diagnostic Parameter's name. This name should uniquely identify this parameter within a project.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The default value for this parameter.
        /// </summary>
        public int DefaultValue { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">The Diagnotic Parameter's name</param>
        /// <param name="defaultValue">A default value for the parameter</param>
        public DiagnosticParameterAttribute(string name, int defaultValue)
        {
            Name = name;
            DefaultValue = defaultValue;
        }
    }
}
