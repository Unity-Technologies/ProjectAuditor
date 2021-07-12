using System;
using System.Collections.Generic;
using Mono.Cecil;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine.Profiling;

namespace Unity.ProjectAuditor.Editor.CodeAnalysis
{
    class CallInfo
    {
        public MethodReference callee;
        public MethodReference caller;
        public Location location;
        public bool perfCriticalContext;
    }

    class CallCrawler
    {
        const int k_MaxDepth = 10;

        readonly Dictionary<string, List<CallInfo>> m_BucketedCallPairs =
            new Dictionary<string, List<CallInfo>>();

        readonly Dictionary<string, CallInfo> m_CallPairs = new Dictionary<string, CallInfo>();

        public void Add(CallInfo callInfo)
        {
            var key = string.Concat(callInfo.caller, "->", callInfo.callee);
            if (!m_CallPairs.ContainsKey(key))
            {
                m_CallPairs.Add(key, callInfo);
            }
        }

        public void BuildCallHierarchies(List<ProjectIssue> issues, IProgress progress = null)
        {
            foreach (var entry in m_CallPairs)
            {
                if (!m_BucketedCallPairs.ContainsKey(entry.Value.callee.FullName))
                    m_BucketedCallPairs.Add(entry.Value.callee.FullName, new List<CallInfo>());
                m_BucketedCallPairs[entry.Value.callee.FullName].Add(entry.Value);
            }

            if (issues.Count > 0)
            {
                Profiler.BeginSample("CallCrawler.BuildCallHierarchies");

                if (progress != null)
                    progress.Start("Analyzing Scripts", "Analyzing call trees", issues.Count);

                foreach (var issue in issues)
                {
                    if (progress != null)
                        progress.Advance();

                    const int depth = 0;
                    var callTree = issue.dependencies;
                    BuildHierarchy(callTree.GetChild() as CallTreeNode, depth);

                    // temp fix for null location (ScriptAuditor was unable to get sequence point)
                    if (issue.location == null && callTree.HasChildren())
                    {
                        issue.location = callTree.GetChild().location;
                    }
                }
                if (progress != null)
                    progress.Clear();

                Profiler.EndSample();
            }
        }

        void BuildHierarchy(CallTreeNode callee, int depth)
        {
            if (depth++ == k_MaxDepth)
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
                        callee.AddChild(callerInstance);
                    }
            }
        }
    }
}
