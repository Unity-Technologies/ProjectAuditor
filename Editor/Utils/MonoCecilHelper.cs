using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Utils
{
    internal static class MonoCecilHelper
    {
        private static readonly int monoBehaviourHashCode = "UnityEngine.MonoBehaviour".GetHashCode();
        
        public static IEnumerable<TypeDefinition> AggregateAllTypeDefinitions(IEnumerable<TypeDefinition> types)
        {
            var typeDefs = types.ToList();
            foreach (var typeDefinition in types)
            {
                if (typeDefinition.HasNestedTypes)
                    typeDefs.AddRange(AggregateAllTypeDefinitions(typeDefinition.NestedTypes));
            }
            return typeDefs;
        }
        
        public static bool IsMonoBehaviour(TypeReference typeReference)
        {
            var typeDefinition = typeReference.Resolve();

            if (typeDefinition.FullName.GetHashCode() == monoBehaviourHashCode && typeDefinition.Module.Name.Equals("UnityEngine.CoreModule.dll"))
                return true;

            if (typeDefinition.BaseType != null)
                return IsMonoBehaviour(typeDefinition.BaseType);

            return false;
        }
    }
}