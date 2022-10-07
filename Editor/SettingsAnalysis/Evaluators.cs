using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    static class Evaluators
    {
        static readonly GraphicsTier[] k_GraphicsTiers = { GraphicsTier.Tier1, GraphicsTier.Tier2, GraphicsTier.Tier3};

        public static bool PlayerSettingsAccelerometerFrequency(BuildTarget platform)
        {
            return PlayerSettings.accelerometerFrequency != 0;
        }

        public static bool PlayerSettingsGraphicsAPIs_Android_Vulkan(BuildTarget platform)
        {
            return !PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).Contains(GraphicsDeviceType.Vulkan);
        }

        public static bool PlayerSettingsGraphicsAPIs_iOS_OpenGLES(BuildTarget platform)
        {
            var graphicsAPIs = PlayerSettings.GetGraphicsAPIs(BuildTarget.iOS);

            var hasMetal = graphicsAPIs.Contains(GraphicsDeviceType.Metal);

            return !hasMetal;
        }

#if UNITY_2020_2_OR_NEWER
        public static bool PlayerSettingsPhysics2DSimulationMode_NotScript(BuildTarget platform)
        {
            return Physics2D.simulationMode != SimulationMode2D.Script;
        }

#endif

        public static bool PlayerSettingsAudioSettings_SpeakerMode(BuildTarget platform)
        {
            return AudioSettings.speakerMode != AudioSpeakerMode.Mono;
        }

        public static bool PlayerSettingsGraphicsAPIs_iOS_OpenGLESAndMetal(BuildTarget platform)
        {
            var graphicsAPIs = PlayerSettings.GetGraphicsAPIs(BuildTarget.iOS);

            var hasOpenGLES = graphicsAPIs.Contains(GraphicsDeviceType.OpenGLES2) ||
                graphicsAPIs.Contains(GraphicsDeviceType.OpenGLES3);

            return graphicsAPIs.Contains(GraphicsDeviceType.Metal) && hasOpenGLES;
        }

        public static bool PlayerSettingsArchitecture_iOS(BuildTarget platform)
        {
            // PlayerSettings.GetArchitecture returns an integer value associated with the architecture of a BuildTargetPlatformGroup. 0 - None, 1 - ARM64, 2 - Universal.
            return PlayerSettings.GetArchitecture(BuildTargetGroup.iOS) == 2;
        }

        public static bool PlayerSettingsArchitecture_Android(BuildTarget platform)
        {
            return (PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARMv7) != 0 &&
                (PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) != 0;
        }

        public static bool PlayerSettingsManagedCodeStripping_iOS(BuildTarget platform)
        {
            var value = PlayerSettings.GetManagedStrippingLevel(BuildTargetGroup.iOS);
            return value == ManagedStrippingLevel.Disabled || value == ManagedStrippingLevel.Low;
        }

        public static bool PlayerSettingsManagedCodeStripping_Android(BuildTarget platform)
        {
            var value = PlayerSettings.GetManagedStrippingLevel(BuildTargetGroup.Android);
            return value == ManagedStrippingLevel.Disabled || value == ManagedStrippingLevel.Low;
        }

        public static bool PlayerSettingsIsStaticBatchingEnabled(BuildTarget platform)
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

        public static bool PlayerSettingsSplashScreenIsEnabledAndCanBeDisabled(BuildTarget platform)
        {
            if (!PlayerSettings.SplashScreen.show)
                return false;
            var type = Type.GetType("UnityEditor.PlayerSettingsSplashScreenEditor,UnityEditor.dll");
            if (type == null)
                return false;

            var licenseAllowsDisablingProperty = type.GetProperty("licenseAllowsDisabling", BindingFlags.Static | BindingFlags.NonPublic);
            if (licenseAllowsDisablingProperty == null)
                return false;
            return (bool)licenseAllowsDisablingProperty.GetValue(null, null);
        }

        public static bool PhysicsLayerCollisionMatrix(BuildTarget platform)
        {
            const int numLayers = 32;
            for (var i = 0; i < numLayers; ++i)
                for (var j = 0; j < i; ++j)
                    if (Physics.GetIgnoreLayerCollision(i, j))
                        return false;
            return true;
        }

        public static bool Physics2DLayerCollisionMatrix(BuildTarget platform)
        {
            const int numLayers = 32;
            for (var i = 0; i < numLayers; ++i)
                for (var j = 0; j < i; ++j)
                    if (Physics2D.GetIgnoreLayerCollision(i, j))
                        return false;
            return true;
        }

        public static bool QualityUsingDefaultSettings(BuildTarget platform)
        {
            return QualitySettings.names.Length == 6 &&
                QualitySettings.names[0] == "Very Low" &&
                QualitySettings.names[1] == "Low" &&
                QualitySettings.names[2] == "Medium" &&
                QualitySettings.names[3] == "High" &&
                QualitySettings.names[4] == "Very High" &&
                QualitySettings.names[5] == "Ultra";
        }

        public static bool QualityUsingLowQualityTextures(BuildTarget platform)
        {
            var usingLowTextureQuality = false;
            var initialQualityLevel = QualitySettings.GetQualityLevel();

            for (var i = 0; i < QualitySettings.names.Length; ++i)
            {
                QualitySettings.SetQualityLevel(i);

#if UNITY_2022_2_OR_NEWER
                if (QualitySettings.globalTextureMipmapLimit > 0)
#else
                if (QualitySettings.masterTextureLimit > 0)
#endif
                {
                    usingLowTextureQuality = true;
                    break;
                }
            }

            QualitySettings.SetQualityLevel(initialQualityLevel);
            return usingLowTextureQuality;
        }

        public static bool QualityDefaultAsyncUploadTimeSlice(BuildTarget platform)
        {
            var usingDefaultAsyncUploadTimeslice = false;
            var initialQualityLevel = QualitySettings.GetQualityLevel();

            for (var i = 0; i < QualitySettings.names.Length; ++i)
            {
                QualitySettings.SetQualityLevel(i);

                if (QualitySettings.asyncUploadTimeSlice == 2)
                {
                    usingDefaultAsyncUploadTimeslice = true;
                    break;
                }
            }

            QualitySettings.SetQualityLevel(initialQualityLevel);
            return usingDefaultAsyncUploadTimeslice;
        }

        public static bool QualityDefaultAsyncUploadBufferSize(BuildTarget platform)
        {
            var usingDefaultAsyncUploadBufferSize = false;
            var initialQualityLevel = QualitySettings.GetQualityLevel();

            for (var i = 0; i < QualitySettings.names.Length; ++i)
            {
                QualitySettings.SetQualityLevel(i);

                if (QualitySettings.asyncUploadBufferSize == 4 || QualitySettings.asyncUploadBufferSize == 16)
                {
                    usingDefaultAsyncUploadBufferSize = true;
                    break;
                }
            }

            QualitySettings.SetQualityLevel(initialQualityLevel);
            return usingDefaultAsyncUploadBufferSize;
        }

        public static bool GraphicsMixedStandardShaderQuality_WithBuiltinRenderPipeline(BuildTarget platform)
        {
            // Only check for Built-In Rendering Pipeline
            if (!GraphicsUsingBuiltinRenderPipeline())
            {
                return false;
            }

            var buildGroup = BuildPipeline.GetBuildTargetGroup(platform);
            var standardShaderQualities = k_GraphicsTiers.Select(tier => EditorGraphicsSettings.GetTierSettings(buildGroup, tier).standardShaderQuality);

            return standardShaderQualities.Distinct().Count() > 1;
        }

        public static bool GraphicsUsingForwardRendering_WithBuiltinRenderPipeline(BuildTarget platform)
        {
            // Only check for Built-In Rendering Pipeline
            if (!GraphicsUsingBuiltinRenderPipeline())
            {
                return false;
            }

            var buildGroup = BuildPipeline.GetBuildTargetGroup(platform);
            var renderingPaths = k_GraphicsTiers.Select(tier => EditorGraphicsSettings.GetTierSettings(buildGroup, tier).renderingPath);

            return renderingPaths.Any(path => path == RenderingPath.Forward);
        }

        public static bool GraphicsUsingDeferredRendering_WithBuiltinRenderPipeline(BuildTarget platform)
        {
            // Only check for Built-In Rendering Pipeline
            if (!GraphicsUsingBuiltinRenderPipeline())
            {
                return false;
            }

            var buildGroup = BuildPipeline.GetBuildTargetGroup(platform);
            var renderingPaths = k_GraphicsTiers.Select(tier => EditorGraphicsSettings.GetTierSettings(buildGroup, tier).renderingPath);

            return renderingPaths.Any(path => path == RenderingPath.DeferredShading);
        }

        static bool GraphicsUsingBuiltinRenderPipeline()
        {
#if UNITY_2019_3_OR_NEWER
            return GraphicsSettings.defaultRenderPipeline == null;
#else
            return true;
#endif
        }
    }
}
