using System.Collections.Generic;

namespace Unity.ProjectAuditor.Editor
{
    public class CallTreeNode
    {
        public string name;

        public List<CallTreeNode> children = new List<CallTreeNode>();

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
    }
}