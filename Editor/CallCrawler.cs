using System.Collections.Generic;
using Mono.Cecil;

namespace Unity.ProjectAuditor.Editor
{
    internal class CallPair
    {
        public MethodReference callee;
        public MethodReference caller;
    }   
    
    internal class CallCrawler
    {
        private Dictionary<string, CallPair> m_CallPairs = new Dictionary<string, CallPair>();
        private Dictionary<string, List<CallPair>> m_BucketedCallPairs = new Dictionary<string, List<CallPair>>();

        private const int m_MaxDepth = 10;
        
        public void Add(MethodReference caller, MethodReference callee)
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
        
        public void BuildCallHierarchies(ProjectReport projectReport, IProgressBar progressBar = null)
        {
            foreach (var entry in m_CallPairs)
            {
                if (!m_BucketedCallPairs.ContainsKey(entry.Value.callee.FullName))                    
                    m_BucketedCallPairs.Add(entry.Value.callee.FullName, new List<CallPair>());
                m_BucketedCallPairs[entry.Value.callee.FullName].Add(entry.Value);                
            }

            var numIssues = projectReport.GetNumIssues(IssueCategory.ApiCalls);
            if (numIssues > 0)
            {
                var issues = projectReport.GetIssues(IssueCategory.ApiCalls);
                if (progressBar != null)
                    progressBar.Initialize("Analyzing Scripts", "Analyzing call trees", numIssues);

                foreach (var issue in issues)
                {
                    if (progressBar != null)
                        progressBar.AdvanceProgressBar();

                    int depth = 0;
                    BuildHierarchy(issue.callTree.GetChild(), depth);
                }
                if (progressBar != null)
                    progressBar.ClearProgressBar();                
            }
        }
        
        private void BuildHierarchy(CallTreeNode callee, int depth)
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
                    if (!call.caller.FullName.Equals(callee.name))
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