using System.Collections.Generic;

namespace Unity.ProjectAuditor.Editor
{
    public class CallInstance
    {
        public string name;

        public List<CallInstance> children = new List<CallInstance>();

        public CallInstance caller
        {
            get
            {
                return children[0];
            }
        }

        public CallInstance(string _name)
        {
            name = _name;
        }

        public CallInstance(string _name, CallInstance caller)
        {
            name = _name;
            children.Add(caller); 
        }
    }
}