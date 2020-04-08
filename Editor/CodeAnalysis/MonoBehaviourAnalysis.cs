using System.Linq;
using Mono.Cecil;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.CodeAnalysis
{
    internal static class MonoBehaviourAnalysis
    {
        private static readonly int ClassNameHashCode = "UnityEngine.MonoBehaviour".GetHashCode();

        private static readonly string[] MagicMethodNames =
        {"Awake", "Start", "OnEnable", "OnDisable", "Update", "LateUpdate", "OnEnable", "FixedUpdate"};

        private static readonly string[] UpdateMethodNames = {"Update", "LateUpdate", "FixedUpdate"};

        public static bool IsMonoBehaviour(TypeReference typeReference)
        {
            try
            {
                var typeDefinition = typeReference.Resolve();

                if (typeDefinition.FullName.GetHashCode() == ClassNameHashCode &&
                    typeDefinition.Module.Name.Equals("UnityEngine.CoreModule.dll"))
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

        public static bool IsMonoBehaviourMagicMethod(MethodDefinition methodDefinition)
        {
            return MagicMethodNames.Contains(methodDefinition.Name);
        }

        public static bool IsMonoBehaviourUpdateMethod(MethodDefinition methodDefinition)
        {
            return UpdateMethodNames.Contains(methodDefinition.Name);
        }
    }
}
