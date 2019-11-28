using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace Unity.ProjectAuditor.Editor
{
    [Serializable]
    public class CallTreeNode
    {
        public string name;
        public string typeName;
        public string methodName;

        public List<CallTreeNode> children = new List<CallTreeNode>();

        public string prettyName
        {
            get
            {
                if (string.IsNullOrEmpty(typeName))
                    return name;
                return typeName + "." + methodName;
            }
        }
        
        public CallTreeNode(string _name, CallTreeNode caller = null)
        {
            name = _name;

            typeName = string.Empty;
            methodName = string.Empty;

            if (caller != null)
                children.Add(caller); 
        }
        
        public CallTreeNode(MethodReference methodReference, CallTreeNode caller = null)
        {
            name = methodReference.FullName;
            methodName = "(anonymous)"; // default value
            
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
        }

        public bool HasChildren()
        {
            return children != null && children.Count > 0;
        }
        
        public CallTreeNode GetChild(int index = 0)
        {
            return children[index];
        }
    }
}