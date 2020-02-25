using System.Linq;
using Mono.Cecil;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Utils
{
    internal static class MonoBehaviourAnalysis
    {
        private static readonly int classNameHashCode = "UnityEngine.MonoBehaviour".GetHashCode();

        private static string[] magicMethodNames = new[]
            {"Awake", "Start", "OnEnable", "OnDisable", "Update", "LateUpdate", "OnEnable", "FixedUpdate"};

        private static string[] updateMethodNames = new[]
            {"Update", "LateUpdate", "FixedUpdate"};

        public static bool IsMonoBehaviour(TypeReference typeReference)
        {
            TypeDefinition typeDefinition = null;
            try
            {
                typeDefinition = typeReference.Resolve();
            }
            catch (AssemblyResolutionException e)
            {
                Debug.LogWarning(e);
                return false;
            }

            if (typeDefinition.FullName.GetHashCode() == classNameHashCode && typeDefinition.Module.Name.Equals("UnityEngine.CoreModule.dll"))
                return true;

            if (typeDefinition.BaseType != null)
                return IsMonoBehaviour(typeDefinition.BaseType);

            return false;
        }

        public static bool IsMonoBehaviourMagicMethod(MethodDefinition methodDefinition)
        {
            return magicMethodNames.Contains(methodDefinition.Name);
        }
        
        public static bool IsMonoBehaviourUpdateMethod(MethodDefinition methodDefinition)
        {
            return updateMethodNames.Contains(methodDefinition.Name);
        }
    }
}