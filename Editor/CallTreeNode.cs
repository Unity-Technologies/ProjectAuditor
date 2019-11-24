using System.Collections.Generic;

namespace Unity.ProjectAuditor.Editor
{
    public class CallTreeNode
    {
        public string name;

        public List<CallTreeNode> children = new List<CallTreeNode>();

        public string prettyName
        {
            get
            {
                // check if it's a coroutine
                if (name.IndexOf("/<") >= 0)
                {
                    var startIndex = name.IndexOf("/<") + 2;
                    var length = name.IndexOf(">") - startIndex;
                    return name.Substring(startIndex, length);
                }

                return name.Substring(name.IndexOf(" "));
            }
        }
        
        public CallTreeNode caller
        {
            get
            {
                return children[0];
            }
        }

        public CallTreeNode(string _name)
        {
            name = _name;
        }

        public CallTreeNode(string _name, CallTreeNode caller)
        {
            name = _name;
            children.Add(caller); 
        }

        public CallTreeNode GetChild(int index = 0)
        {
            return children[0];
        }
    }
}