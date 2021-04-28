using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.Editor.SettingsAnalyzers
{
    class Evaluators
    {
        readonly GraphicsTier[] m_GraphicsTiers = {GraphicsTier.Tier1, GraphicsTier.Tier2, GraphicsTier.Tier3};

        public bool PlayerSettingsAccelerometerFrequency()
        {
            return PlayerSettings.accelerometerFrequency != 0;
        }

        public bool PlayerSettingsGraphicsAPIs_iOS_OpenGLES()
        {
            var graphicsAPIs = PlayerSettings.GetGraphicsAPIs(BuildTarget.iOS);

            var hasMetal = graphicsAPIs.Contains(GraphicsDeviceType.Metal);

            return !hasMetal;
        }

        public bool PlayerSettingsGraphicsAPIs_iOS_OpenGLESAndMetal()
        {
            var graphicsAPIs = PlayerSettings.GetGraphicsAPIs(BuildTarget.iOS);

            var hasOpenGLES = graphicsAPIs.Contains(GraphicsDeviceType.OpenGLES2) ||
                graphicsAPIs.Contains(GraphicsDeviceType.OpenGLES3);

            return graphicsAPIs.Contains(GraphicsDeviceType.Metal) && hasOpenGLES;
        }

        public bool PlayerSettingsArchitecture_iOS()
        {
            // PlayerSettings.GetArchitecture returns an integer value associated with the architecture of a BuildTargetPlatformGroup. 0 - None, 1 - ARM64, 2 - Universal.
            return PlayerSettings.GetArchitecture(BuildTargetGroup.iOS) == 2;
        }

        public bool PlayerSettingsArchitecture_Android()
        {
            return (PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARMv7) != 0 &&
                (PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) != 0;
        }

        public bool PlayerSettingsManagedCodeStripping_iOS()
        {
#if UNITY_2018_3_OR_NEWER
            var value = PlayerSettings.GetManagedStrippingLevel(BuildTargetGroup.iOS);
            return value == ManagedStrippingLevel.Disabled || value == ManagedStrippingLevel.Low;
#else
            return false;
#endif
        }

        public bool PlayerSettingsManagedCodeStripping_Android()
        {
#if UNITY_2018_3_OR_NEWER
            var value = PlayerSettings.GetManagedStrippingLevel(BuildTargetGroup.Android);
            return value == ManagedStrippingLevel.Disabled || value == ManagedStrippingLevel.Low;
#else
            return false;
#endif
        }

        public bool PlayerSettingsSplashScreenIsEnabledAndCanBeDisabled()
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

        public bool PhysicsLayerCollisionMatrix()
        {
            const int numLayers = 32;
            for (var i = 0; i < numLayers; ++i)
                for (var j = 0; j < i; ++j)
                    if (Physics.GetIgnoreLayerCollision(i, j))
                        return false;
            return true;
        }

        public bool Physics2DLayerCollisionMatrix()
        {
            const int numLayers = 32;
            for (var i = 0; i < numLayers; ++i)
                for (var j = 0; j < i; ++j)
                    if (Physics2D.GetIgnoreLayerCollision(i, j))
                        return false;
            return true;
        }

        public bool QualityUsingDefaultSettings()
        {
            return QualitySettings.names.Length == 6 &&
                QualitySettings.names[0] == "Very Low" &&
                QualitySettings.names[1] == "Low" &&
                QualitySettings.names[2] == "Medium" &&
                QualitySettings.names[3] == "High" &&
                QualitySettings.names[4] == "Very High" &&
                QualitySettings.names[5] == "Ultra";
        }

        public bool QualityUsingLowQualityTextures()
        {
            var usingLowTextureQuality = false;
            var initialQualityLevel = QualitySettings.GetQualityLevel();

            for (var i = 0; i < QualitySettings.names.Length; ++i)
            {
                QualitySettings.SetQualityLevel(i);

                if (QualitySettings.masterTextureLimit > 0)
                {
                    usingLowTextureQuality = true;
                    break;
                }
            }

            QualitySettings.SetQualityLevel(initialQualityLevel);
            return usingLowTextureQuality;
        }

        public bool QualityDefaultAsyncUploadTimeSlice()
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

        public bool QualityDefaultAsyncUploadBufferSize()
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

        public bool GraphicsMixedStandardShaderQuality()
        {
            var buildGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var standardShaderQualities = m_GraphicsTiers.Select(tier => EditorGraphicsSettings.GetTierSettings(buildGroup, tier).standardShaderQuality);

            return standardShaderQualities.Distinct().Count() > 1;
        }

        public bool GraphicsUsingForwardRendering()
        {
            var buildGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var renderingPaths = m_GraphicsTiers.Select(tier => EditorGraphicsSettings.GetTierSettings(buildGroup, tier).renderingPath);

            return renderingPaths.Any(path => path == RenderingPath.Forward);
        }

        public bool GraphicsUsingDeferredRendering()
        {
            var buildGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var renderingPaths = m_GraphicsTiers.Select(tier => EditorGraphicsSettings.GetTierSettings(buildGroup, tier).renderingPath);

            return renderingPaths.Any(path => path == RenderingPath.DeferredShading);
        }
    }
}
