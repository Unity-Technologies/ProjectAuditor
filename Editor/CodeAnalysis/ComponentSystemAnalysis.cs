using System.Linq;
using Mono.Cecil;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.CodeAnalysis
{
    static class ComponentSystemAnalysis
    {
        static readonly int[] ClassNameHashCodes = {"Unity.Entities.ComponentSystem".GetHashCode(), "Unity.Entities.JobComponentSystem".GetHashCode()};

        static readonly string[] UpdateMethodNames = {"OnUpdate"};

        public static bool IsComponentSystem(TypeReference typeReference)
        {
            try
            {
                var typeDefinition = typeReference.Resolve();

                if (ClassNameHashCodes.FirstOrDefault(i => i == typeDefinition.FullName.GetHashCode()) != 0 &&
                    typeDefinition.Module.Name.Equals("Unity.Entities.dll"))
                    return true;

                if (typeDefinition.BaseType != null)
                    return IsComponentSystem(typeDefinition.BaseType);
            }
            catch (AssemblyResolutionException e)
            {
                Debug.LogWarning(e);
            }

            return false;
        }

        public static bool IsOnUpdateMethod(MethodDefinition methodDefinition)
        {
            return UpdateMethodNames.Contains(methodDefinition.Name);
        }
    }
}
