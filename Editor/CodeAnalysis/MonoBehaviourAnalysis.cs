using System;
using System.Linq;
using Mono.Cecil;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.CodeAnalysis
{
    static class MonoBehaviourAnalysis
    {
        static readonly int k_CoreModuleHashCode = "UnityEngine.CoreModule.dll".GetHashCode();
        static readonly int k_MonoBehaviourHashCode = "UnityEngine.MonoBehaviour".GetHashCode();
        static readonly int k_ILPostProcessorHashCode = "Unity.CompilationPipeline.Common.ILPostProcessing.ILPostProcessor".GetHashCode();

        static readonly string[] k_EventNames =
        {"Awake", "Start", "OnEnable", "OnDisable", "Update", "LateUpdate", "OnEnable", "FixedUpdate"};

        static readonly string[] k_UpdateMethodNames = {"Update", "LateUpdate", "FixedUpdate"};

        public static bool IsMonoBehaviour(TypeReference typeReference)
        {
            // handle special case where Assembly will fail to be Resolved
            if (typeReference.FullName.GetHashCode() == k_ILPostProcessorHashCode)
                return false;

            try
            {
                var typeDefinition = typeReference.Resolve();

                if (typeDefinition.FullName.GetHashCode() == k_MonoBehaviourHashCode &&
                    typeDefinition.Module.Name.GetHashCode() == k_CoreModuleHashCode)
                    return true;

                if (typeDefinition.BaseType != null)
                    return IsMonoBehaviour(typeDefinition.BaseType);
            }
            catch (AssemblyResolutionException e)
            {
                Debug.LogWarning(e);
            }

            return false;
        }

        public static bool IsMonoBehaviourEvent(MethodDefinition methodDefinition)
        {
            return k_EventNames.Contains(methodDefinition.Name);
        }

        public static bool IsMonoBehaviourUpdateMethod(MethodDefinition methodDefinition)
        {
            return k_UpdateMethodNames.Contains(methodDefinition.Name);
        }
    }
}
