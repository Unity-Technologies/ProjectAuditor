using System.Collections.Generic;
using Mono.Cecil;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.Editor.CodeAnalysis
{
    internal class CallPair
    {
        public MethodReference callee;
        public MethodReference caller;
        public Location location;
        public bool perfCriticalContext;
    }

    internal class CallCrawler
    {
        private const int m_MaxDepth = 10;

        private readonly Dictionary<string, List<CallPair>> m_BucketedCallPairs =
            new Dictionary<string, List<CallPair>>();

        private readonly Dictionary<string, CallPair> m_CallPairs = new Dictionary<string, CallPair>();

        public void Add(CallPair callPair)
        {
            var key = string.Concat(callPair.caller, "->", callPair.callee);
            if (!m_CallPairs.ContainsKey(key))
            {
                m_CallPairs.Add(key, callPair);
            }
        }

        public void BuildCallHierarchies(List<ProjectIssue> issues, IProgressBar progressBar = null)
        {
            foreach (var entry in m_CallPairs)
            {
                if (!m_BucketedCallPairs.ContainsKey(entry.Value.callee.FullName))
                    m_BucketedCallPairs.Add(entry.Value.callee.FullName, new List<CallPair>());
                m_BucketedCallPairs[entry.Value.callee.FullName].Add(entry.Value);
            }

            if (issues.Count > 0)
            {
                if (progressBar != null)
                    progressBar.Initialize("Analyzing Scripts", "Analyzing call trees", issues.Count);

                foreach (var issue in issues)
                {
                    if (progressBar != null)
                        progressBar.AdvanceProgressBar();

                    var depth = 0;
                    var callTree = issue.callTree;
                    BuildHierarchy(callTree.GetChild(), depth);

                    // temp fix for null location (ScriptAuditor was unable to get sequence point)
                    if (issue.location == null && callTree.HasChildren())
                    {
                        issue.location = callTree.GetChild().location;
                    }
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
                    // ignore recursive calls
                    if (!call.caller.FullName.Equals(callee.name))
                    {
                        var callerInstance = new CallTreeNode(call.caller);
                        callerInstance.location = call.location;
                        callerInstance.perfCriticalContext = call.perfCriticalContext;

                        BuildHierarchy(callerInstance, depth);
                        callee.children.Add(callerInstance);
                    }
            }
        }
    }
}
