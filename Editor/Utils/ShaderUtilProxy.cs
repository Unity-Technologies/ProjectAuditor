using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.Editor.Utils
{
    static class ShaderUtilProxy
    {
#pragma warning disable 0414
        static Type s_TypeShaderUtil;
        static MethodInfo s_MethodGetAvailableShaderCompilerPlatforms;
        static MethodInfo s_MethodGetShaderVariantCount;
        static MethodInfo s_MethodGetShaderGlobalKeywords;
        static MethodInfo s_MethodGetShaderLocalKeywords;
        static MethodInfo s_MethodGetShaderActiveSubshaderIndex;
        static MethodInfo s_MethodGetSRPBatcherCompatibilityCode;
        static MethodInfo s_MethodHasInstancing;
#pragma warning restore 0414

        static string[] s_ShaderPlatformNames;

        static void Init()
        {
            s_TypeShaderUtil = typeof(ShaderUtil);
            s_MethodGetAvailableShaderCompilerPlatforms = s_TypeShaderUtil.GetMethod("GetAvailableShaderCompilerPlatforms", BindingFlags.Static | BindingFlags.NonPublic);
            s_MethodGetShaderActiveSubshaderIndex = s_TypeShaderUtil.GetMethod("GetShaderActiveSubshaderIndex", BindingFlags.Static | BindingFlags.NonPublic);
            s_MethodGetShaderGlobalKeywords = s_TypeShaderUtil.GetMethod("GetShaderGlobalKeywords", BindingFlags.Static | BindingFlags.NonPublic);
            s_MethodGetShaderLocalKeywords = s_TypeShaderUtil.GetMethod("GetShaderLocalKeywords", BindingFlags.Static | BindingFlags.NonPublic);
            s_MethodGetShaderVariantCount = s_TypeShaderUtil.GetMethod("GetVariantCount", BindingFlags.Static | BindingFlags.NonPublic);
            s_MethodGetSRPBatcherCompatibilityCode = s_TypeShaderUtil.GetMethod("GetSRPBatcherCompatibilityCode", BindingFlags.Static | BindingFlags.NonPublic);
            s_MethodHasInstancing = s_TypeShaderUtil.GetMethod("HasInstancing", BindingFlags.Static | BindingFlags.NonPublic);

            var platformMask = (int)s_MethodGetAvailableShaderCompilerPlatforms.Invoke(null, new object[] { });
            var names = new List<string>();
            for (int i = 0; i < 32; ++i)
            {
                if ((platformMask & (1 << i)) == 0)
                    continue;
                names.Add(((UnityEditor.Rendering.ShaderCompilerPlatform)i).ToString());
            }
            s_ShaderPlatformNames = names.ToArray();
        }

        // note that this method is not present in ShaderUtil
        public static string[] GetCompilerPlatformNames()
        {
            if (s_ShaderUtilType == null)
                Init();

            return s_ShaderPlatformNames;
        }

        public static int GetShaderActiveSubshaderIndex(Shader shader)
        {
            if (s_ShaderUtilType == null)
                Init();

            if (s_GetShaderActiveSubshaderIndex == null)
                return 0;

            return (int)s_GetShaderActiveSubshaderIndex.Invoke(null, new object[] { shader});
        }

        public static string[] GetShaderGlobalKeywords(Shader shader)
        {
            if (s_ShaderUtilType == null)
                Init();

            if (s_GetShaderGlobalKeywordsMethod == null)
                return null;

            return (string[])s_GetShaderGlobalKeywordsMethod.Invoke(null, new object[] { shader});
        }

        public static string[] GetShaderLocalKeywords(Shader shader)
        {
            if (s_ShaderUtilType == null)
                Init();

            if (s_GetShaderLocalKeywordsMethod == null)
                return null;

            return (string[])s_GetShaderLocalKeywordsMethod.Invoke(null, new object[] { shader});
        }

        public static int GetSRPBatcherCompatibilityCode(Shader shader, int subShaderIdx)
        {
#if UNITY_2019_1_OR_NEWER
            if (s_ShaderUtilType == null)
                Init();

            if (s_GetSRPBatcherCompatibilityCode == null)
                return -1;
            if (RenderPipelineManager.currentPipeline == null)
                return -1;
            return (int)s_GetSRPBatcherCompatibilityCode.Invoke(null, new object[] { shader, subShaderIdx});
#else
            return -1;
#endif
        }

        public static int GetVariantCount(Shader shader)
        {
            if (s_ShaderUtilType == null)
                Init();

            if (s_GetShaderVariantCountMethod == null)
                return 0;

            return (int)s_GetShaderVariantCountMethod.Invoke(null, new object[] { shader});
        }

        public static bool HasInstancing(Shader shader)
        {
            if (s_ShaderUtilType == null)
                Init();

            if (s_HasInstancingMethod == null)
                return false;

            return (bool)s_HasInstancingMethod.Invoke(null, new object[] { shader});
        }
    }
}
