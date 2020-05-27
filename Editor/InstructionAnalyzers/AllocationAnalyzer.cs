using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.Auditors;
using Unity.ProjectAuditor.Editor.CodeAnalysis;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    internal class AllocationAnalyzer : IInstructionAnalyzer
    {
        private static readonly ProblemDescriptor s_ObjectAllocationDescriptor = new ProblemDescriptor
            (
            102002,
            "Object Allocation (experimental)",
            Area.Memory,
            "An object is allocated in managed memory",
            "Try to avoid allocating objects in frequently-updated code."
            );

        private static readonly ProblemDescriptor s_ArrayAllocationDescriptor = new ProblemDescriptor
            (
            102003,
            "Array Allocation (experimental)",
            Area.Memory,
            "An array is allocated in managed memory",
            "Try to avoid allocating arrays in frequently-updated code."
            );

        public void Initialize(IAuditor auditor)
        {
            auditor.RegisterDescriptor(s_ObjectAllocationDescriptor);
            auditor.RegisterDescriptor(s_ArrayAllocationDescriptor);
        }

        public ProjectIssue Analyze(MethodDefinition methodDefinition, Instruction inst)
        {
            if (inst.OpCode == OpCodes.Newobj)
            {
                var methodReference = (MethodReference)inst.Operand;
                var typeReference = methodReference.DeclaringType;
                if (typeReference.IsValueType)
                    return null;

                var descriptor = s_ObjectAllocationDescriptor;
                var description = string.Format("'{0}' object allocation", typeReference.Name);

                var calleeNode = new CallTreeNode(methodDefinition);

                return new ProjectIssue
                (
                    descriptor,
                    description,
                    IssueCategory.ApiCalls,
                    calleeNode
                );
            }
            else // OpCodes.Newarr
            {
                var typeReference = (TypeReference)inst.Operand;
                var descriptor = s_ArrayAllocationDescriptor;
                var description = string.Format("'{0}' array allocation", typeReference.Name);

                var calleeNode = new CallTreeNode(methodDefinition);

                return new ProjectIssue
                (
                    descriptor,
                    description,
                    IssueCategory.ApiCalls,
                    calleeNode
                );
            }
        }

        public IEnumerable<OpCode> GetOpCodes()
        {
            yield return OpCodes.Newobj;
            yield return OpCodes.Newarr;
        }
    }
}
