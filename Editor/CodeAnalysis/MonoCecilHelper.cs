using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Unity.ProjectAuditor.Editor.CodeAnalysis
{
    static class MonoCecilHelper
    {
        // for reference https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/MonoCecil/MonoCecilHelper.cs
        public static IEnumerable<TypeDefinition> AggregateAllTypeDefinitions(IEnumerable<TypeDefinition> types)
        {
            var typeDefs = types.ToList();
            foreach (var typeDefinition in types)
                if (typeDefinition.HasNestedTypes)
                    typeDefs.AddRange(AggregateAllTypeDefinitions(typeDefinition.NestedTypes));
            return typeDefs;
        }
    }
}
