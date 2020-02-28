using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.Editor.CodeAnalysis
{
    [Serializable]
    public class CallTreeNode
    {
        public string assemblyName;

        public List<CallTreeNode> children = new List<CallTreeNode>();
        public Location location;
        public string methodName;
        public string name;
        public bool perfCriticalContext;
        public string typeName;

        public CallTreeNode(MethodReference methodReference, CallTreeNode caller = null)
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
                children.Add(caller);
            perfCriticalContext = false;
        }

        public string prettyName => GetPrettyName();

        public bool HasChildren()
        {
            return children != null && children.Count > 0;
        }

        public void AddChild(CallTreeNode child)
        {
            children.Add(child);
        }

        public CallTreeNode GetChild(int index = 0)
        {
            return children[index];
        }

        public string GetPrettyName(bool withAssembly = false)
        {
            if (string.IsNullOrEmpty(typeName))
                return name;
            if (string.IsNullOrEmpty(assemblyName) || !withAssembly)
                return string.Format("{0}.{1}", typeName, methodName);
            return string.Format("{0}.{1} [{2}]", typeName, methodName, assemblyName);
        }

        public bool IsPerfCriticalContext()
        {
            return perfCriticalContext || children.Any(child => child.IsPerfCriticalContext());
        }
    }
}