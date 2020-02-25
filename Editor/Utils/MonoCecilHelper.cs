using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Utils
{
    internal static class MonoCecilHelper
    {
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
    }
}