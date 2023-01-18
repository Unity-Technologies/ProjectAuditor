using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using UnityEditor;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.Editor.Modules
{
    class FogModeAnalyzer : ISettingsModuleAnalyzer
    {
        private static readonly Descriptor k_FogModeDescriptor = new Descriptor(
            "PAS1003",
            "Graphics: Fog Mode",
            new[] {Area.BuildSize},
            "Enabling Fog Stripping will result in additional shader variants, thus increasing build size.",
            "To reduce the build size, switch Fog Modes at <b>Edit ➔ Project Settings ➔ Graphics ➔ Fog Modes</b> to <b>Automatic</b>.")
        {
            fixer = (issue =>
            {
                RemoveFogStripping();
            })
        };

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_FogModeDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams)
        {
            if (IsFogStrippingCustom())
            {
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_FogModeDescriptor)
                    .WithLocation("Project/Graphics");
            }
        }

        internal static bool IsFogStrippingCustom()
        {
            var serializedObject = new SerializedObject(GraphicsSettings.GetGraphicsSettings());
            var mode = serializedObject.FindProperty("m_FogStripping").enumValueIndex;

            //As we can't access the enum from here, we can't cast it and check the value
            //1 is for "Custom" - 0 for "Automatic"
            if (mode == 1)
            {
                return true;
            }

            return false;
        }

        internal static void RemoveFogStripping()
        {
            var serializedObject = new SerializedObject(GraphicsSettings.GetGraphicsSettings());
            serializedObject.FindProperty("m_FogStripping").enumValueIndex = 0;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
