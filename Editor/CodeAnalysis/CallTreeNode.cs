using System;
using System.Linq;
using Mono.Cecil;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.CodeAnalysis
{
    public class CallTreeNode : DependencyNode
    {
        public string assemblyName;
        public string methodName;
        public string name;
        public string typeName;

        internal CallTreeNode(MethodReference methodReference, CallTreeNode caller = null)
        {
            name = methodReference.FullName;
            methodName = "(anonymous)"; // default value
            assemblyName = methodReference.Module.Name;

            // check if it's a coroutine
            if (methodReference.DeclaringType.FullName.IndexOf("/<") >= 0)
            {
                var fullName = methodReference.DeclaringType.FullName;
                var methodStartIndex = fullName.IndexOf("<") + 1;
                if (methodStartIndex > 0)
                {
                    var length = fullName.IndexOf(">") - methodStartIndex;
                    typeName = fullName.Substring(0, fullName.IndexOf("/"));
                    if (length > 0)
                    {
                        methodName = fullName.Substring(methodStartIndex, length);
                    }
                    else
                    {
                        // handle example: System.Int32 DelegateTest/<>c::<Update>b__1_0()
                        methodStartIndex = name.LastIndexOf("<") + 1;
                        if (methodStartIndex > 0)
                        {
                            length = name.LastIndexOf(">") - methodStartIndex;
                            methodName = name.Substring(methodStartIndex, length) + ".(anonymous)";
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

        // string GetPrettyName(bool withAssembly = false)
        public override string GetPrettyName()
        {
            if (string.IsNullOrEmpty(typeName))
                return name;
            return string.Format("{0}.{1}", typeName, methodName);
        }

        public override bool IsPerfCritical()
        {
            return perfCriticalContext || (HasChildren() && m_Children.Any(child => child.IsPerfCritical()));
        }
    }
}
