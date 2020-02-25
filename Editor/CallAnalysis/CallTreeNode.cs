using System;
using System.Collections.Generic;
using Mono.Cecil;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.Editor.CallAnalysis
{
    [Serializable]
    public class CallTreeNode
    {
        public string name;
        public string typeName;
        public string methodName;
        public string assemblyName;
        public Location location;
        public bool perfCriticalContext;
        
        public List<CallTreeNode> children = new List<CallTreeNode>();

        public string prettyName
        {
            get
            {
                return GetPrettyName();
            }
        }

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
                        methodName = fullName.Substring(methodStartIndex, length);
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
            {
                return string.Format("{0}.{1}", typeName, methodName);
            }
            return string.Format("{0}.{1} [{2}]", typeName, methodName, assemblyName);
        }

        public bool IsPerfCriticalContext()
        {
            if (perfCriticalContext)
                return true;

            bool value = false;
            foreach (var child in children)
            {
                if (child.IsPerfCriticalContext())
                {
                    value = true;
                    break;
                }
            }

            return value;
        }
    }
}