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

        static readonly ProblemDescriptor k_ClosureAllocationDescriptor = new ProblemDescriptor
            (
            102003,
            "Closure Allocation",
            Area.Memory,
            "An object is allocated in managed memory",
            "Try to avoid allocating objects in frequently-updated code."
            );


        static readonly ProblemDescriptor k_ArrayAllocationDescriptor = new ProblemDescriptor
            (
            102004,
            "Array Allocation",
            Area.Memory,
            "An array is allocated in managed memory",
            "Try to avoid allocating arrays in frequently-updated code."
            );

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_ObjectAllocationDescriptor);
            module.RegisterDescriptor(k_ClosureAllocationDescriptor);
            module.RegisterDescriptor(k_ArrayAllocationDescriptor);
        }

        public ProjectIssue Analyze(MethodDefinition methodDefinition, Instruction inst)
        {
            if (inst.OpCode == OpCodes.Newobj)
            {
                var methodReference = (MethodReference)inst.Operand;
                var typeReference = methodReference.DeclaringType;
                if (typeReference.IsValueType)
                    return null;

                var calleeNode = new CallTreeNode(methodDefinition);
                var isClosure = typeReference.Name.StartsWith("<>c__DisplayClass");
                if (isClosure)
                {
                    return new ProjectIssue
                    (
                        k_ClosureAllocationDescriptor,
                        string.Format("'{0}' closure allocation", typeReference.DeclaringType.FullName),
                        IssueCategory.Code,
                        calleeNode
                    );
                }

                return new ProjectIssue
                (
                    k_ObjectAllocationDescriptor,
                    string.Format("'{0}' allocation", typeReference.FullName),
                    IssueCategory.Code,
                    calleeNode
                );
            }
            else // OpCodes.Newarr
            {
                var typeReference = (TypeReference)inst.Operand;
                var description = string.Format("'{0}' array allocation", typeReference.Name);

                var calleeNode = new CallTreeNode(methodDefinition);

                return new ProjectIssue
                (
                    k_ArrayAllocationDescriptor,
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
