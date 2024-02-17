using System;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    internal static class EditorInterop
    {
        public static void CopyToClipboard(string text)
        {
            EditorGUIUtility.systemCopyBuffer = text;
        }

        public static void OpenCodeDescriptor(Descriptor descriptor)
        {
            const string prefix = "UnityEngine.";
            if (descriptor.Type.StartsWith(prefix))
            {
                var unityVersion = InternalEditorUtility.GetUnityVersion();
                var type = descriptor.Type.Substring(prefix.Length);
                var method = descriptor.Method;
                var url = string.Format("https://docs.unity3d.com/{0}.{1}/Documentation/ScriptReference/{2}{3}{4}.html",
                    unityVersion.Major, unityVersion.Minor, type, Char.IsUpper(method[0]) ? "." : "-", method);
                Application.OpenURL(url);
            }
        }

        public static void OpenCompilerMessageDescriptor(Descriptor descriptor)
        {
            const string prefix = "CS";
            const string baseURL = "https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/";
            if (descriptor.Title.StartsWith(prefix))
            {
                Application.OpenURL(baseURL + descriptor.Title);
            }
        }

        public static void OpenTextFile<T>(Location location) where T : UnityEngine.Object
        {
            var obj = AssetDatabase.LoadAssetAtPath<T>(location.Path);
            if (obj != null)
            {
                // open text file in the text editor
                AssetDatabase.OpenAsset(obj, location.Line);
            }
        }

        public static void OpenPackage(Location location)
        {
            var packageName = Path.GetFileName(location.Path);
            UnityEditor.PackageManager.UI.Window.Open(packageName);
        }

        public static void OpenProjectSettings(Location location)
        {
            if (location.Path.Equals("Project/Build"))
                BuildPlayerWindow.ShowBuildPlayerWindow();
            else
            {
                // Some Quality setting issue paths will end with the quality level name to identify a specific level
                // However, the SettingsService API does not support this, so we need to strip the level name
                var path = location.Path.StartsWith("Project/Quality") ? "Project/Quality" : location.Path;
                var window = SettingsService.OpenProjectSettings(path);
                window.Repaint();
            }
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

        public static void OpenProjectAuditorPreferences()
        {
            var window = UserPreferences.OpenPreferencesWindow();
            window.Repaint();
        }
    }
}
