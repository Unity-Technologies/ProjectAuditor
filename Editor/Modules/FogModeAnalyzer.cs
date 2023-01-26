using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using UnityEditor;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.Editor.Modules
{
    public enum FogMode
    {
        Linear,
        Exponential,
        ExponentialSquarred,
        Automatic
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

            messageFormat = "Graphics: FogMode {0} shader variants are always included in the build."
        };

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_FogModeDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams)
        {
            if (!IsFogStrippingEnabled(FogMode.Automatic))
            {
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_FogModeDescriptor, GetFogModesEnabledString())
                    .WithLocation("Project/Graphics");
            }
        }

        static string GetFogModesEnabledString()
        {
            var serializedObject = new SerializedObject(GraphicsSettings.GetGraphicsSettings());
            string message = "";

            var linearFog = serializedObject.FindProperty("m_FogKeepLinear").boolValue;
            var expFog = serializedObject.FindProperty("m_FogKeepExp").boolValue;
            var exp2Fog = serializedObject.FindProperty("m_FogKeepExp2").boolValue;

            if (linearFog) message += "- Linear ";
            if (expFog) message += "- Exponential ";
            if (exp2Fog) message += "- Exponential Squarred ";

            return message;
        }

        internal static bool IsFogStrippingEnabled(FogMode fogMode)
        {
            var serializedObject = new SerializedObject(GraphicsSettings.GetGraphicsSettings());
            bool isEnabled = false;

            switch (fogMode)
            {
                case FogMode.Automatic:
                    isEnabled = serializedObject.FindProperty("m_FogStripping").enumValueIndex == 0; //Automatic mode
                    break;

                case FogMode.Exponential:
                    isEnabled = serializedObject.FindProperty("m_FogKeepExp").boolValue;
                    break;

                case FogMode.ExponentialSquarred:
                    isEnabled = serializedObject.FindProperty("m_FogKeepExp2").boolValue;
                    break;

                case FogMode.Linear:
                    isEnabled = serializedObject.FindProperty("m_FogKeepLinear").boolValue;
                    break;
            }

            return isEnabled;
        }


        internal static void RemoveFogStripping()
        {
            var serializedObject = new SerializedObject(GraphicsSettings.GetGraphicsSettings());
            serializedObject.FindProperty("m_FogStripping").enumValueIndex = 0;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
