using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Utils
{
    public static class PlayerSettingsUtil
    {
        internal static SerializedObject GetPlayerSettingsSerializedObject()
        {
            var internalGetPlayerSettingsMethod = typeof(PlayerSettings).GetMethod("InternalGetPlayerSettingsObject", BindingFlags.Static | BindingFlags.NonPublic);
            if (internalGetPlayerSettingsMethod == null)
                return null;

            var playerSettings = internalGetPlayerSettingsMethod.Invoke(null, null);
            if (playerSettings == null)
                return null;

            return new SerializedObject(playerSettings as UnityEngine.Object);
        }

        public static int GetVertexChannelCompressionMask()
        {
            var serializedSettings = GetPlayerSettingsSerializedObject();

            var compressionFlagsProperty = serializedSettings.FindProperty("VertexChannelCompressionMask");
            if (compressionFlagsProperty == null)
                return 0;

            return compressionFlagsProperty.intValue;
        }

        public static void SetVertexChannelCompressionMask(int newValue)
        {
            var serializedSettings = GetPlayerSettingsSerializedObject();

            var compressionFlagsProperty = serializedSettings.FindProperty("VertexChannelCompressionMask");
            if (compressionFlagsProperty == null)
                return;

            serializedSettings.Update();

            compressionFlagsProperty.intValue = newValue;

            serializedSettings.ApplyModifiedProperties();
        }

        public static bool IsStaticBatchingEnabled(BuildTarget platform)
        {
            var method = typeof(PlayerSettings).GetMethod("GetBatchingForPlatform",
                BindingFlags.Static | BindingFlags.Default | BindingFlags.NonPublic);
            if (method == null)
                throw new NotSupportedException("Getting batching per platform is not supported");

            const int staticBatching = 0;
            const int dynamicBatching = 0;
            var args = new object[]
            {
                platform,
                staticBatching,
                dynamicBatching
            };

            method.Invoke(null, args);
            return (int)args[1] > 0;
        }

        public static bool IsLightmapStreamingEnabled(BuildTargetGroup platform)
        {
            var method = typeof(PlayerSettings).GetMethod("GetLightmapStreamingEnabledForPlatformGroup",
                BindingFlags.Static | BindingFlags.Default | BindingFlags.NonPublic);
            if (method == null)
                throw new NotSupportedException("Getting Lightmap Streaming per platform is not supported");

            var returnValue = method.Invoke(null, new object[]{platform});

            if (returnValue == null)
                throw new NotSupportedException("Getting Lightmap Streaming per platform is not supported");

            return (bool)returnValue;
        }

        public static void SetLightmapStreaming(BuildTargetGroup platform, bool value)
        {
            var method = typeof(PlayerSettings).GetMethod("SetLightmapStreamingEnabledForPlatformGroup",
                BindingFlags.Static | BindingFlags.Default | BindingFlags.NonPublic);
            if (method == null)
                throw new NotSupportedException("Setting Lightmap Streaming per platform is not supported");

            method.Invoke(null, new object[]{platform, value});
        }

#if UNITY_2021_2_OR_NEWER
        public static void GetDefaultTextureCompressionFormat(BuildTargetGroup buildTargetGroup, out int formatEnumIndex, out Array formatEnumValues)
        {
            formatEnumValues = default;
            formatEnumIndex = -1;

            var method = typeof(PlayerSettings).GetMethod("GetDefaultTextureCompressionFormat",
                BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null)
                throw new NotSupportedException("PlayerSettings.GetDefaultTextureCompressionFormat method is not supported");

            var format = method.Invoke(null, new object[] { buildTargetGroup });

            var enumType = format.GetType();
            formatEnumValues = Enum.GetValues(enumType);
            formatEnumIndex = (int)format;
        }

#endif
    }
}
