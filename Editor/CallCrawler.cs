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
        private Dictionary<string, List<CallPair>> m_BucketedCallPairs = new Dictionary<string, List<CallPair>>();

        private const int m_MaxDepth = 10;
        
        public void Add(string caller, string callee)
        {
            var key = string.Concat(caller, "->", callee);
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
            foreach (var entry in m_CallPairs)
            {
                if (!m_BucketedCallPairs.ContainsKey(entry.Value.callee))                    
                    m_BucketedCallPairs.Add(entry.Value.callee, new List<CallPair>());
                m_BucketedCallPairs[entry.Value.callee].Add(entry.Value);                
            }
            
            var issues = projectReport.GetIssues(IssueCategory.ApiCalls);
            var progressBar =
                new ProgressBarDisplay("Analyzing Scripts", "Analyzing call trees", issues.Count);

            foreach (var issue in issues)
            {
                progressBar.AdvanceProgressBar();

                int depth = 0;
                BuildHierarchy(issue.callTree.caller, depth);
            }
            progressBar.ClearProgressBar();
        }
        
        public void BuildHierarchy(CallTreeNode callee, int depth)
        {
            if (depth++ == m_MaxDepth)
                return;

            // let's find all callers with matching callee
            if (m_BucketedCallPairs.ContainsKey(callee.name))
            {
                var callPairs = m_BucketedCallPairs[callee.name];

                foreach (var call in callPairs)
                {
                    // ignore recursive calls
                    if (!call.caller.Equals(callee.name))
                    {
                        var callerInstance = new CallTreeNode(call.caller);
                        BuildHierarchy(callerInstance, depth);
                        callee.children.Add(callerInstance); 
                    }    
                }  
            }
        }
    }
}