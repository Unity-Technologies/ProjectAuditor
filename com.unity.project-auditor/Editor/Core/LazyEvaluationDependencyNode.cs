using System;

namespace Unity.ProjectAuditor.Editor.Core
{
    internal class LazyEvaluationDependencyNode : SimpleDependencyNode
    {
        public Action OnEvaluate { get; set; }

        public LazyEvaluationDependencyNode(string name) : base(name)
        {
        }
    }
}
