using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.CodeAnalysis
{
    public class CallTreeNode
    {
        private List<CallTreeNode> m_Children = new List<CallTreeNode>();

        public string assemblyName;
        public Location location;
        public string methodName;
        public string name;
        public bool perfCriticalContext;
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

        public string prettyName
        {
            get { return GetPrettyName(); }
        }

        public bool HasChildren()
        {
            return m_Children != null && m_Children.Count > 0;
        }

        public void AddChild(CallTreeNode child)
        {
            m_Children.Add(child);
        }

        public CallTreeNode GetChild(int index = 0)
        {
            return m_Children[index];
        }

        public int GetNumChildren()
        {
            return m_Children.Count;
        }

        public bool HasValidChildren()
        {
            return m_Children != null;
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
            return perfCriticalContext || (HasChildren() && m_Children.Any(child => child.IsPerfCriticalContext()));
        }
    }
}
