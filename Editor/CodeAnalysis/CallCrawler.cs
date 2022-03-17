using System;
using System.Collections.Generic;
using Mono.Cecil;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine.Profiling;

namespace Unity.ProjectAuditor.Editor.CodeAnalysis
{
    class CallInfo
    {
        public readonly MethodReference callee;
        public readonly MethodReference caller;
        public readonly Location location;
        public readonly bool perfCriticalContext;
        public CallTreeNode hierarchy;

        public CallInfo(
            MethodReference callee,
            MethodReference caller,
            Location location,
            bool perfCriticalContext)
        {
            this.callee = callee;
            this.caller = caller;
            this.location = location;
            this.perfCriticalContext = perfCriticalContext;
        }

        public override bool Equals(object obj)
        {
            var other = obj as CallInfo;
            if (other == null)
            {
                return false;
            }

            return other.callee == callee &&
                other.caller == caller;
        }

        public override int GetHashCode()
        {
            return callee.GetHashCode()
                + caller.GetHashCode();
        }
    }

    class CallCrawler
    {
        const int k_MaxDepth = 10;

        readonly Dictionary<string, List<CallInfo>> m_BucketedCalls =
            new Dictionary<string, List<CallInfo>>();

        readonly HashSet<CallInfo> m_Calls = new HashSet<CallInfo>();

        public void Add(CallInfo callInfo)
        {
            m_Calls.Add(callInfo);
        }

        public void BuildCallHierarchies(List<ProjectIssue> issues, IProgress progress = null)
        {
            foreach (var callInfo in m_Calls)
            {
                var key = callInfo.callee.FullName;
                List<CallInfo> calls;
                if (!m_BucketedCalls.TryGetValue(key, out calls))
                {
                    calls = new List<CallInfo>();
                    m_BucketedCalls.Add(key, calls);
                }
                calls.Add(callInfo);
            }

            if (issues.Count > 0)
            {
                Profiler.BeginSample("CallCrawler.BuildCallHierarchies");

                if (progress != null)
                    progress.Start("Analyzing Method calls", string.Empty, issues.Count);

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
            List<CallInfo> callPairs;
            if (m_BucketedCalls.TryGetValue(callee.name, out callPairs))
            {
                var childrenCount = callPairs.Count;
                for (int i = 0; i < childrenCount; i++)
                {
                    var call = callPairs[i];
                    // ignore recursive calls
                    if (!call.caller.FullName.Equals(callee.name))
                    {
                        if (call.hierarchy != null)
                        {
                            // use previously built hierarchy
                            callee.AddChild(call.hierarchy);
                        }
                        else
                        {
                            var hierarchy = new CallTreeNode(call.caller);
                            hierarchy.location = call.location;
                            hierarchy.perfCriticalContext = call.perfCriticalContext;

                            BuildHierarchy(hierarchy, depth);
                            callee.AddChild(hierarchy);
                            call.hierarchy = hierarchy;
                        }
                    }
                }
            }
        }
    }
}
