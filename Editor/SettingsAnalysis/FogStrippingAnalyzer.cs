using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Modules
{
    enum FogStripping
    {
        Automatic,
        Custom
    }

    enum FogMode
    {
        Linear,
        Exponential,
        ExponentialSquared
    }

    class FogStrippingAnalyzer : ISettingsModuleAnalyzer
    {
        internal const string PAS1003 = nameof(PAS1003);

        static readonly Descriptor k_FogModeDescriptor = new Descriptor(
            PAS1003,
            "Graphics: Fog Shader Variant Stripping",
            new[] {Area.BuildSize},
            "<b>Fog Modes</b> in Graphics Settings are set to build all fog shader variants. Forcing Fog shader variants to be built can increase the build size.",
            "Change <b>Graphics Settings ➔ Fog Modes</b> to <b>Automatic</b> or disable <b>Linear/Exponential/Exponential Squared</b>. This should reduce the number of shader variants generated for fog effects.")
        {
            fixer = (issue =>
            {
                RemoveFogStripping();
            }),

            messageFormat = "Graphics: FogMode '{0}' shader variants is always included in the build"
        };

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_FogModeDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams)
        {
            if (IsFogModeEnabled(FogMode.Linear))
            {
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_FogModeDescriptor, FogMode.Linear)
                    .WithLocation("Project/Graphics");
            }

            if (IsFogModeEnabled(FogMode.Exponential))
            {
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_FogModeDescriptor, FogMode.Exponential)
                    .WithLocation("Project/Graphics");
            }

            if (IsFogModeEnabled(FogMode.ExponentialSquared))
            {
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_FogModeDescriptor, FogMode.ExponentialSquared)
                    .WithLocation("Project/Graphics");
            }
        }

        internal static bool IsFogModeEnabled(FogMode fogMode)
        {
            var graphicsSettings = GraphicsSettingsProxy.GetGraphicsSettings();
            var serializedObject = new SerializedObject(graphicsSettings);

            if (FogStripping.Automatic == (FogStripping)serializedObject.FindProperty("m_FogStripping").enumValueIndex)
                return false;

            switch (fogMode)
            {
                case FogMode.Exponential:
                    return serializedObject.FindProperty("m_FogKeepExp").boolValue;

                case FogMode.ExponentialSquared:
                    return serializedObject.FindProperty("m_FogKeepExp2").boolValue;

                case FogMode.Linear:
                    return serializedObject.FindProperty("m_FogKeepLinear").boolValue;
            }

            return false;
        }

        internal static void RemoveFogStripping()
        {
            var graphicsSettings = GraphicsSettingsProxy.GetGraphicsSettings();
            var serializedObject = new SerializedObject(graphicsSettings);

            serializedObject.FindProperty("m_FogStripping").enumValueIndex = (int)FogStripping.Automatic;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
