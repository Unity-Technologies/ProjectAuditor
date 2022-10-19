using System;
using System.Linq;
using Mono.Cecil;
using Unity.ProjectAuditor.Editor.Core;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.CodeAnalysis
{
    public class CallTreeNode : DependencyNode
    {
        internal readonly string m_Name;

        public readonly string assemblyName;
        public readonly string methodName;
        public readonly string typeName;

        internal CallTreeNode(MethodReference methodReference, CallTreeNode caller = null)
        {
            m_Name = methodReference.FullName;
            methodName = "(anonymous)"; // default value
            assemblyName = methodReference.Module.Name;

            // check if it's a coroutine
            if (methodReference.DeclaringType.FullName.IndexOf("/<", StringComparison.Ordinal) >= 0)
            {
                var fullName = methodReference.DeclaringType.FullName;
                var methodStartIndex = fullName.IndexOf("<", StringComparison.Ordinal) + 1;
                if (methodStartIndex > 0)
                {
                    var length = fullName.IndexOf(">", StringComparison.Ordinal) - methodStartIndex;
                    typeName = fullName.Substring(0, fullName.IndexOf("/", StringComparison.Ordinal));
                    if (length > 0)
                    {
                        methodName = fullName.Substring(methodStartIndex, length);
                    }
                    else
                    {
                        // handle example: System.Int32 DelegateTest/<>c::<Update>b__1_0()
                        methodStartIndex = m_Name.LastIndexOf("<") + 1;
                        if (methodStartIndex > 0)
                        {
                            length = m_Name.LastIndexOf(">") - methodStartIndex;
                            methodName = m_Name.Substring(methodStartIndex, length) + ".(anonymous)";
                        }
                    }
                }
                else
                {
                    // for some reason, some generated types don't have the same syntax
                    typeName = fullName;
                }
            }
            else
            {
                typeName = methodReference.DeclaringType.Name;
                methodName = methodReference.Name;
            }

            if (caller != null)
                AddChild(caller);
            perfCriticalContext = false;
        }

        public override string GetName()
        {
            return m_Name;
        }

        public override string GetPrettyName()
        {
            if (string.IsNullOrEmpty(typeName))
                return m_Name;
            return string.Format("{0}.{1}", typeName, methodName);
        }

        public override bool IsPerfCritical()
        {
            return perfCriticalContext;
        }
    }
}
