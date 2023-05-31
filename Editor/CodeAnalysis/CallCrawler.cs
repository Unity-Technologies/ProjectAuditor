using System;
using System.Collections.Generic;
using Mono.Cecil;
using Unity.ProjectAuditor.Editor.Core;
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

        // key: callee name, value: lists of all callers
        readonly Dictionary<string, List<CallInfo>> m_BucketedCalls =
            new Dictionary<string, List<CallInfo>>();

        public void Add(CallInfo callInfo)
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

        public void BuildCallHierarchies(List<ProjectIssue> issues, IProgress progress = null)
        {
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
                    var root = issue.dependencies;
                    BuildHierarchy(root as CallTreeNode, depth);

                    // temp fix for null location (code analysis was unable to get sequence point)
                    if (issue.location == null)
                        issue.location = root.location;
                }
                if (progress != null)
                    progress.Clear();

                Profiler.EndSample();
            }
        }

        void BuildHierarchy(CallTreeNode callee, int depth)
        {
            // this check should be removed. Instead, the deep callstacks should be built on-demand
            if (depth++ == k_MaxDepth)
                return;

            // let's find all callers with matching callee
            List<CallInfo> callPairs;
            if (m_BucketedCalls.TryGetValue(callee.methodFullName, out callPairs))
            {
                var childrenCount = callPairs.Count;
                var children = new DependencyNode[childrenCount];

                for (var i = 0; i < childrenCount; i++)
                {
                    var call = callPairs[i];
                    if (call.hierarchy != null)
                    {
                        // use previously built hierarchy
                        children[i] = call.hierarchy;
                        continue;
                    }

                    var callerName = call.caller.FullName;
                    var hierarchy = new CallTreeNode(call.caller)
                    {
                        location = call.location, perfCriticalContext = call.perfCriticalContext
                    };

                    // stop recursion, if applicable (note that this only prevents recursion when a method calls itself)
                    if (!callerName.Equals(callee.methodFullName))
                        BuildHierarchy(hierarchy, depth);

                    children[i] = hierarchy;
                    call.hierarchy = hierarchy;
                }
                callee.AddChildren(children);
            }
        }
    }
}
