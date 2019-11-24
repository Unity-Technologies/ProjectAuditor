using System.Collections.Generic;
using Mono.Cecil;

namespace Unity.ProjectAuditor.Editor
{
    public class CallTreeNode
    {
        public readonly string name;
        public readonly string typeName;
        public readonly string methodName;

        public List<CallTreeNode> children = new List<CallTreeNode>();

        public string prettyName
        {
            get
            {
                return typeName + "." + methodName;
            }
        }
        
        public CallTreeNode(MethodReference methodReference, CallTreeNode caller = null)
        {
            name = methodReference.FullName;

            // check if it's a coroutine
            if (name.IndexOf("/<") >= 0)
            {
                var fullName = methodReference.DeclaringType.FullName;
                var methodStartIndex = fullName.IndexOf("<") + 1;
                var length = fullName.IndexOf(">") - methodStartIndex;
                typeName = fullName.Substring(0, fullName.IndexOf("/"));
                methodName = fullName.Substring(methodStartIndex, length);
            }
            else
            {
                typeName = methodReference.DeclaringType.Name;
                methodName = methodReference.Name;
            }

            if (caller != null)
                children.Add(caller); 
        }

        public CallTreeNode GetChild(int index = 0)
        {
            return children[0];
        }
    }
}