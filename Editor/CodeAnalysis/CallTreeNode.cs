using System;
using System.Linq;
using Mono.Cecil;
using Unity.ProjectAuditor.Editor.Core;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.CodeAnalysis
{
    internal class CallTreeNode : DependencyNode
    {
        /// <summary>
        /// Assembly name
        /// </summary>
        public readonly string assemblyName;

        /// <summary>
        /// Full name of the type, including namespace
        /// </summary>
        public readonly string typeFullName;

        /// <summary>
        /// Full name of the method, including parameters and return type
        /// </summary>
        public readonly string methodFullName;

        /// <summary>
        /// User-friendly name of the type
        /// </summary>
        public readonly string prettyTypeName;

        /// <summary>
        /// User-friendly name of the method
        /// </summary>
        public readonly string prettyMethodName;

        public CallTreeNode(MethodReference methodReference, CallTreeNode caller = null)
        {
            methodFullName = methodReference.FullName;
            typeFullName = methodReference.DeclaringType.FullName;
            prettyMethodName = "(anonymous)"; // default value
            assemblyName = methodReference.Module.Name;

            // check if it's a coroutine
            if (methodReference.DeclaringType.FullName.IndexOf("/<", StringComparison.Ordinal) >= 0)
            {
                var fullName = methodReference.DeclaringType.FullName;
                var methodStartIndex = fullName.IndexOf("<", StringComparison.Ordinal) + 1;
                if (methodStartIndex > 0)
                {
                    var length = fullName.IndexOf(">", StringComparison.Ordinal) - methodStartIndex;
                    prettyTypeName = fullName.Substring(0, fullName.IndexOf("/", StringComparison.Ordinal));
                    if (length > 0)
                    {
                        prettyMethodName = fullName.Substring(methodStartIndex, length);
                    }
                    else
                    {
                        // handle example: System.Int32 DelegateTest/<>c::<Update>b__1_0()
                        methodStartIndex = methodFullName.LastIndexOf("<") + 1;
                        if (methodStartIndex > 0)
                        {
                            length = methodFullName.LastIndexOf(">") - methodStartIndex;
                            prettyMethodName = methodFullName.Substring(methodStartIndex, length) + ".(anonymous)";
                        }
                    }
                }
                else
                {
                    // for some reason, some generated types don't have the same syntax
                    prettyTypeName = fullName;
                }
            }
            else
            {
                prettyTypeName = methodReference.DeclaringType.Name;
                prettyMethodName = methodReference.Name;
            }

            if (caller != null)
                AddChild(caller);
            perfCriticalContext = false;
        }

        public override string GetName()
        {
            return methodFullName;
        }

        public override string GetPrettyName()
        {
            if (string.IsNullOrEmpty(prettyTypeName))
                return methodFullName;
            return string.Format("{0}.{1}", prettyTypeName, prettyMethodName);
        }

        public override bool IsPerfCritical()
        {
            return perfCriticalContext;
        }
    }
}
