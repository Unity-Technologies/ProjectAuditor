using System;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    public static class EditorUtil
    {
        public static void OpenCodeDescriptor(ProblemDescriptor descriptor)
        {
            var unityVersion = InternalEditorUtility.GetUnityVersion();
            if (unityVersion.Major < 2017)
                return;

            const string prefix = "UnityEngine.";
            if (descriptor.type.StartsWith(prefix))
            {
                var type = descriptor.type.Substring(prefix.Length);
                var method = descriptor.method;
                var url = string.Format("https://docs.unity3d.com/{0}.{1}/Documentation/ScriptReference/{2}{3}{4}.html",
                    unityVersion.Major, unityVersion.Minor, type, Char.IsUpper(method[0]) ? "." : "-", method);
                Application.OpenURL(url);
            }
        }

        public static void OpenCompilerMessageDescriptor(ProblemDescriptor descriptor)
        {
            const string prefix = "CS";
            const string baseURL = "https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/";
            if (descriptor.description.StartsWith(prefix))
            {
                Application.OpenURL(baseURL + descriptor.description);
            }
        }

        public static void OpenTextFile(Location location)
        {
            var obj = AssetDatabase.LoadAssetAtPath<TextAsset>(location.Path);
            if (obj != null)
            {
                // open text file in the text editor
                AssetDatabase.OpenAsset(obj, location.Line);
            }
        }

        public static void OpenProjectSettings(Location location)
        {
#if UNITY_2018_3_OR_NEWER
            var window = SettingsService.OpenProjectSettings(location.Path);
            window.Repaint();
#endif
        }

        public static void FocusOnAssetInProjectWindow(Location location)
        {
            // Note that LoadMainAssetAtPath might fails, for example if there is a compile error in the script associated with the asset.
            //
            // Instead, we should use GetMainAssetInstanceID and FrameObjectInProjectWindow internal methods:
            //    var instanceId = AssetDatabase.GetMainAssetInstanceID(location.Path);
            //    ProjectWindowUtil.FrameObjectInProjectWindow(instanceId);

            var obj = AssetDatabase.LoadMainAssetAtPath(location.Path);
            if (obj != null)
            {
                ProjectWindowUtil.ShowCreatedAsset(obj);
            }
        }
    }
}
