using System;
using System.Reflection;
using UnityEditor;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.Editor.Utils
{
    static class GraphicsSettingsProxy
    {
        static PropertyInfo s_LogWhenShaderIsCompiled;

        public static bool logShaderCompilationSupported
        {
            get
            {
                return s_LogWhenShaderIsCompiled != null;
            }
        }

        public static bool logWhenShaderIsCompiled
        {
            get
            {
                if (s_LogWhenShaderIsCompiled == null)
                    return false;
                return (bool)s_LogWhenShaderIsCompiled.GetValue(null, null);
            }

            set
            {
                if (s_LogWhenShaderIsCompiled == null)
                    return;
                s_LogWhenShaderIsCompiled.SetValue(null, value, null);
            }
        }

        public static UnityEngine.Object GetGraphicsSettings()
        {
#if UNITY_2020_2_OR_NEWER
            return GraphicsSettings.GetGraphicsSettings();
#else
            var getGraphicsSettings = typeof(GraphicsSettings).GetMethod("GetGraphicsSettings", BindingFlags.Static | BindingFlags.NonPublic);
            return getGraphicsSettings.Invoke(null, null) as UnityEngine.Object;
#endif
        }

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            var graphicsSettings = typeof(GraphicsSettings);
            s_LogWhenShaderIsCompiled = graphicsSettings.GetProperty("logWhenShaderIsCompiled", BindingFlags.Static | BindingFlags.Public);
        }
    }
}
