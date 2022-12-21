using System;
using System.Reflection;
using UnityEditor;

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
