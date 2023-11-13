using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
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
            "Graphics: Fog Mode is enabled",
            new[] {Area.BuildSize},
            "<b>Fog Modes</b> in Graphics Settings are set to build all fog shader variants for this fog mode. Forcing Fog shader variants to be built can increase the build size.",
            "Change <b>Project Settings > Graphics > Fog Modes</b> to <b>Automatic</b> or disable <b>Linear/Exponential/Exponential Squared</b>. This should reduce the number of shader variants generated for fog effects.")
        {
            fixer = (issue =>
            {
                RemoveFogStripping();
            }),

            messageFormat = "Graphics: Fog Mode '{0}' shader variants are always included in the build"
        };

        public void Initialize(Module module)
        {
            module.RegisterDescriptor(k_FogModeDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(SettingsAnalysisContext context)
        {
            if (IsFogModeEnabled(FogMode.Linear))
            {
                yield return context.Create(IssueCategory.ProjectSetting, k_FogModeDescriptor.id, FogMode.Linear)
                    .WithLocation("Project/Graphics");
            }

            if (IsFogModeEnabled(FogMode.Exponential))
            {
                yield return context.Create(IssueCategory.ProjectSetting, k_FogModeDescriptor.id, FogMode.Exponential)
                    .WithLocation("Project/Graphics");
            }

            if (IsFogModeEnabled(FogMode.ExponentialSquared))
            {
                yield return context.Create(IssueCategory.ProjectSetting, k_FogModeDescriptor.id, FogMode.ExponentialSquared)
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
