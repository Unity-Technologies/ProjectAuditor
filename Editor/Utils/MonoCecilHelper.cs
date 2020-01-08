using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Utils
{
    internal static class MonoCecilHelper
    {
        private static readonly int objectHashCode = "System.Object".GetHashCode();
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
        
        public static bool IsMonoBehaviour(TypeDefinition typeDefinition)
        {
            var baseType = typeDefinition.BaseType;
            if (baseType.FullName.GetHashCode() == monoBehaviourHashCode)
                return true;

            if (baseType.FullName.GetHashCode() == objectHashCode)
                return false;

            try
            {
                return IsMonoBehaviour( baseType.Resolve());
            }
            catch (Exception e)
            {
                Debug.Log("Cannot Resolve BaseType " + baseType.FullName);
                return false;
            }
            
        }
    }
}