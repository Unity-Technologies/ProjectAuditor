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
                if (!m_BucketedCalls.ContainsKey(callInfo.callee.FullName))
                    m_BucketedCalls.Add(callInfo.callee.FullName, new List<CallInfo>());
                m_BucketedCalls[callInfo.callee.FullName].Add(callInfo);
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
            if (m_BucketedCalls.ContainsKey(callee.name))
            {
                var callPairs = m_BucketedCalls[callee.name];

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
