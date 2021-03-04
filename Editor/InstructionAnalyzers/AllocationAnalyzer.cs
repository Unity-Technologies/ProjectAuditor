using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.CodeAnalysis;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    class AllocationAnalyzer : IInstructionAnalyzer
    {
        static readonly ProblemDescriptor k_ObjectAllocationDescriptor = new ProblemDescriptor
            (
            102002,
            "Object Allocation",
            Area.Memory,
            "An object is allocated in managed memory",
            "Try to avoid allocating objects in frequently-updated code."
            );

        static readonly ProblemDescriptor k_ArrayAllocationDescriptor = new ProblemDescriptor
            (
            102003,
            "Array Allocation",
            Area.Memory,
            "An array is allocated in managed memory",
            "Try to avoid allocating arrays in frequently-updated code."
            );

        public void Initialize(IAuditor auditor)
        {
            auditor.RegisterDescriptor(k_ObjectAllocationDescriptor);
            auditor.RegisterDescriptor(k_ArrayAllocationDescriptor);
        }

        public ProjectIssue Analyze(MethodDefinition methodDefinition, Instruction inst)
        {
            if (inst.OpCode == OpCodes.Newobj)
            {
                var methodReference = (MethodReference)inst.Operand;
                var typeReference = methodReference.DeclaringType;
                if (typeReference.IsValueType)
                    return null;

                var descriptor = k_ObjectAllocationDescriptor;
                var description = string.Format("'{0}' object allocation", typeReference.Name);

                var calleeNode = new CallTreeNode(methodDefinition);

                return new ProjectIssue
                (
                    descriptor,
                    description,
                    IssueCategory.Code,
                    calleeNode
                );
            }
            else // OpCodes.Newarr
            {
                var typeReference = (TypeReference)inst.Operand;
                var descriptor = k_ArrayAllocationDescriptor;
                var description = string.Format("'{0}' array allocation", typeReference.Name);

                var calleeNode = new CallTreeNode(methodDefinition);

                return new ProjectIssue
                (
                    descriptor,
                    description,
                    IssueCategory.Code,
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
