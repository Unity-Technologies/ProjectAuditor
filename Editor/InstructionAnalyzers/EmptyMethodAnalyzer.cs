using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.CodeAnalysis;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    class EmptyMethodAnalyzer : IInstructionAnalyzer
    {
        static readonly ProblemDescriptor k_Descriptor = new ProblemDescriptor
            (
            102001,
            "Empty MonoBehaviour Method",
            Area.CPU,
            "Any empty MonoBehaviour magic method will be included in the build and executed anyway.",
            "Remove any empty MonoBehaviour methods."
            );

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_Descriptor);
        }

        public ProjectIssue Analyze(MethodDefinition methodDefinition, Instruction inst)
        {
            // skip any no-op
            var previousIL = inst.Previous;
            while (previousIL != null && previousIL.OpCode == OpCodes.Nop)
                previousIL = previousIL.Previous;

            // if there is no instruction before OpCodes.Ret, then we know this method is empty
            if (previousIL != null)
                return null;

            if (!MonoBehaviourAnalysis.IsMonoBehaviour(methodDefinition.DeclaringType))
                return null;

            if (!MonoBehaviourAnalysis.IsMonoBehaviourEvent(methodDefinition))
                return null;

            return new ProjectIssue
            (
                k_Descriptor,
                methodDefinition.FullName,
                IssueCategory.Code,
                new CallTreeNode(methodDefinition)
            );
        }

        public IEnumerable<OpCode> GetOpCodes()
        {
            yield return OpCodes.Ret;
        }

        public static ProblemDescriptor GetDescriptor()
        {
            return k_Descriptor;
        }
    }
}
