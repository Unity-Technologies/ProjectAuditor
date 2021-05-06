#if !UNITY_2019_2_OR_NEWER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Utils
{
    public static class TypeCache
    {
        static List<Type> s_Types;

        static IEnumerable<Type> GetAllTypes()
        {
            if (s_Types != null)
                return s_Types;

            var types = new List<Type>();
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    types.AddRange(a.GetTypes());
                }
                catch (ReflectionTypeLoadException /* e */)
                {
                    Debug.LogWarningFormat("Project Auditor: Could not get {0} types information", a.GetName().Name);
                }
            }

            s_Types = types;

            return types;
        }

        public static IEnumerable<Type> GetTypesDerivedFrom(Type parentType)
        {
            return GetAllTypes()
                .Where(type => type != parentType && parentType.IsAssignableFrom(type));
        }
    }
}
#endif
