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
    }
}
