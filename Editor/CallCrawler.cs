using System.Collections.Generic;
using System.Linq;

namespace Unity.ProjectAuditor.Editor
{
    class CallPair
    {
        public string callee;
        public string caller;
    }   
    
    class CallCrawler
    {
        private Dictionary<string, CallPair> m_CallPairs = new Dictionary<string, CallPair>();

        public void Add(string caller, string callee)
        {
            var key = string.Concat(caller, "->", caller);
            if (!m_CallPairs.ContainsKey(key))
            {
                var calledMethodPair = new CallPair
                {
                    callee = callee,
                    caller = caller
                };
                
                m_CallPairs.Add(key, calledMethodPair);
            }            
        }
        
        public void BuildCallHierarchies(ProjectReport projectReport)
        {
            var issues = projectReport.GetIssues(IssueCategory.ApiCalls);
            foreach (var issue in issues)
            {
                BuildHierarchy(issue.callTree.caller);
            }  
        }
        
        public void BuildHierarchy(CallTreeNode callee)
        {
            // let's find all callers with matching callee
            var callPairs = m_CallPairs.Where(call => call.Value.callee.Equals(callee.name));

            foreach (var call in callPairs)
            {
                // ignore recursive calls
                if (!call.Value.caller.Equals(callee.name))
                {
                    var callerInstance = new CallTreeNode(call.Value.caller);
                    BuildHierarchy(callerInstance);
                    callee.children.Add(callerInstance); 
                }    
            }
        }
    }
}