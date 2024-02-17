#if PACKAGE_ENTITIES
using System;
using System.Linq;
using Mono.Cecil;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.CodeAnalysis
{
    static class ComponentSystemAnalysis
    {
        static readonly int k_EntitiesModuleHashCode = "Unity.Entities.dll".GetHashCode();
        static readonly int[] k_ClassNameHashCodes = {"Unity.Entities.SystemBase".GetHashCode(), "Unity.Entities.ISystem".GetHashCode()};
        static readonly int k_ILPostProcessorHashCode = "Unity.CompilationPipeline.Common.ILPostProcessing.ILPostProcessor".GetHashCode();

        static readonly string[] k_UpdateMethodNames = {"OnUpdate"};

        public static bool IsComponentSystem(TypeReference typeReference)
        {
            // handle special case where Assembly will fail to be Resolved
            if (typeReference.FullName.GetHashCode() == k_ILPostProcessorHashCode)
                return false;

            try
            {
                var typeDefinition = typeReference.Resolve();

                if (k_ClassNameHashCodes.FirstOrDefault(i => i == typeDefinition.FullName.GetHashCode()) != 0 &&
                    typeDefinition.Module.Name.GetHashCode() == k_EntitiesModuleHashCode)
                    return true;

                if (typeDefinition.BaseType != null)
                    return IsComponentSystem(typeDefinition.BaseType);
            }
            catch (AssemblyResolutionException e)
            {
                Debug.LogWarningFormat("Could not resolve {0}: {1}", typeReference.Name, e.Message);
            }

            return false;
        }

        public static bool IsOnUpdateMethod(MethodDefinition methodDefinition)
        {
            return k_UpdateMethodNames.Contains(methodDefinition.Name);
        }
    }
}
#endif
