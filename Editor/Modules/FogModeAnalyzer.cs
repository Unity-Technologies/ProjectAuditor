using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using UnityEditor;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.Editor.Modules
{
    enum FogModeStripping
    {
        Automatic,
        Custom
    }

    public enum FogMode
    {
        Linear,
        Exponential,
        ExponentialSquared
    }

    class FogModeAnalyzer : ISettingsModuleAnalyzer
    {
        private static readonly Descriptor k_FogModeDescriptor = new Descriptor(
            "PAS1003",
            "Graphics: Fog Shader Variant Stripping",
            new[] {Area.BuildSize},
            "FogMode shader variants are always built. Forcing Fog shader variants to be built can increase the build size.",
            "To reduce the number of shader variants, change <b>Edit ➔ Project Settings ➔ Graphics ➔ Fog Modes</b> to <b>Automatic</b> or disable <b>Linear/Exponential/Exponential Squared</b>.")
        {
            fixer = (issue =>
            {
                RemoveFogStripping();
            }),

            messageFormat = "Graphics: FogMode {0} shader variants is always included in the build."
        };

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_FogModeDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams)
        {
            if (IsFogStrippingEnabled(FogMode.Linear))
            {
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_FogModeDescriptor, FogMode.Linear)
                    .WithLocation("Project/Graphics");
            }

            if (IsFogStrippingEnabled(FogMode.Exponential))
            {
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_FogModeDescriptor, FogMode.Exponential)
                    .WithLocation("Project/Graphics");
            }

            if (IsFogStrippingEnabled(FogMode.ExponentialSquared))
            {
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_FogModeDescriptor, FogMode.ExponentialSquared)
                    .WithLocation("Project/Graphics");
            }
        }

        internal static bool IsFogStrippingEnabled(FogMode fogMode)
        {
            var serializedObject = new SerializedObject(GraphicsSettings.GetGraphicsSettings());

            var mode = (FogModeStripping)serializedObject.FindProperty("m_FogStripping").enumValueIndex;

            if (mode == FogModeStripping.Automatic) return false;

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
            var getGraphicsSettings = typeof(GraphicsSettings).GetMethod("GetGraphicsSettings", BindingFlags.Static | BindingFlags.NonPublic);
            var graphicsSettings = getGraphicsSettings.Invoke(null, null) as UnityEngine.Object;
            var serializedObject = new SerializedObject(graphicsSettings);

            serializedObject.FindProperty("m_FogStripping").enumValueIndex = (int)FogModeStripping.Automatic;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
