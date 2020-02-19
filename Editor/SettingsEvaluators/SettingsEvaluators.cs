using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Rendering;

namespace Unity.ProjectAuditor.Editor
{
    internal class SettingsEvaluators
    {
        // Edit this to reflect the target platforms for your project
        // TODO - Provide an interface for this, or something
        BuildTargetGroup[] _buildTargets = {BuildTargetGroup.iOS, BuildTargetGroup.Android, BuildTargetGroup.Standalone};
        GraphicsTier[] _graphicsTiers = {GraphicsTier.Tier1, GraphicsTier.Tier2, GraphicsTier.Tier3};
        
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

        public bool PhysicsLayerCollisionMatrix()
        {
            const int NUM_LAYERS = 32;
            for (int i = 0; i < NUM_LAYERS; ++i)
            {
                for (int j = 0; j < i; ++j)
                {
                    if (Physics.GetIgnoreLayerCollision(i, j))
                    {
                        return false;
                    }
                } 
            }
            return true;
        }

        public bool Physics2DLayerCollisionMatrix()
        {
            const int NUM_LAYERS = 32;
            for (int i = 0; i < NUM_LAYERS; ++i)
            {
                for (int j = 0; j < i; ++j)
                {
                    if (Physics2D.GetIgnoreLayerCollision(i, j))
                    {
                        return false;
                    }
                } 
            }
            return true;
        }

        public bool QualityUsingDefaultSettings()
        {
            return (QualitySettings.names.Length == 6 &&
                    QualitySettings.names[0] == "Very Low" &&
                    QualitySettings.names[1] == "Low" &&
                    QualitySettings.names[2] == "Medium" &&
                    QualitySettings.names[3] == "High" &&
                    QualitySettings.names[4] == "Very High" &&
                    QualitySettings.names[5] == "Ultra");
        }

        public bool QualityUsingLowQualityTextures()
        {
            bool usingLowTextureQuality = false;
            int initialQualityLevel = QualitySettings.GetQualityLevel();
        
            for (int i = 0; i < QualitySettings.names.Length; ++i)
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
            bool usingDefaultAsyncUploadTimeslice = false;
            int initialQualityLevel = QualitySettings.GetQualityLevel();
        
            for (int i = 0; i < QualitySettings.names.Length; ++i)
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
            bool usingDefaultAsyncUploadBufferSize = false;
            int initialQualityLevel = QualitySettings.GetQualityLevel();
        
            for (int i = 0; i < QualitySettings.names.Length; ++i)
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
            for (int btIdx = 0; btIdx < _buildTargets.Length; ++btIdx)
            {
                ShaderQuality ssq = EditorGraphicsSettings.GetTierSettings(_buildTargets[btIdx], _graphicsTiers[0]).standardShaderQuality;
                for (int tierIdx = 0; tierIdx < _graphicsTiers.Length; ++tierIdx)
                {
                    TierSettings tierSettings =
                        EditorGraphicsSettings.GetTierSettings(_buildTargets[btIdx], _graphicsTiers[tierIdx]);

                    if (tierSettings.standardShaderQuality != ssq)
                        return true;
                }
            }
            return false;
        }

        public bool GraphicsUsingForwardRendering()
        {
            for (int btIdx = 0; btIdx < _buildTargets.Length; ++btIdx)
            {
                for (int tierIdx = 0; tierIdx < _graphicsTiers.Length; ++tierIdx)
                {
                    TierSettings tierSettings =
                        EditorGraphicsSettings.GetTierSettings(_buildTargets[btIdx], _graphicsTiers[tierIdx]);

                    if (tierSettings.renderingPath == RenderingPath.Forward)
                        return true;
                }
            }

            return false;
        }

        public bool GraphicsUsingDeferredRendering()
        {
            for (int btIdx = 0; btIdx < _buildTargets.Length; ++btIdx)
            {
                for (int tierIdx = 0; tierIdx < _graphicsTiers.Length; ++tierIdx)
                {
                    TierSettings tierSettings =
                        EditorGraphicsSettings.GetTierSettings(_buildTargets[btIdx], _graphicsTiers[tierIdx]);

                    if (tierSettings.renderingPath == RenderingPath.DeferredShading)
                        return true;
                }
            }

            return false;
        }
        
        
    }
}