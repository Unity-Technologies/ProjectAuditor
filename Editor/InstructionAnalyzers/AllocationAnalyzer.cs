using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.Auditors;
using Unity.ProjectAuditor.Editor.CallAnalysis;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    [Attribute]
    public class AllocationAnalyzer : IInstructionAnalyzer
    {
        private static readonly ProblemDescriptor objectAllocationDescriptor = new ProblemDescriptor
        {
            id = 102002,
            description = "Object Allocation (experimental)",
            type = string.Empty,
            method = string.Empty,
            area = "Memory",
            problem = "An object is allocated in managed memory",
            solution = "Try to avoid allocating objects in frequently-updated code."
        };

        private static readonly ProblemDescriptor arrayAllocationDescriptor = new ProblemDescriptor
        {
            id = 102003,
            description = "Array Allocation (experimental)",
            type = string.Empty,
            method = string.Empty,
            area = "Memory",
            problem = "An array is allocated in managed memory",
            solution = "Try to avoid allocating arrays in frequently-updated code."
        };

        public AllocationAnalyzer(ScriptAuditor auditor)
        {
            auditor.RegisterDescriptor(objectAllocationDescriptor);
            auditor.RegisterDescriptor(arrayAllocationDescriptor);
        }

        public ProjectIssue Analyze(MethodDefinition methodDefinition, Instruction inst)
        {
            if (inst.OpCode == OpCodes.Newobj)
            {
                var methodReference = (MethodReference) inst.Operand;
                var typeReference = methodReference.DeclaringType;
                if (typeReference.IsValueType)
                    return null;

                var descriptor = objectAllocationDescriptor;
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
                var typeReference = (TypeReference) inst.Operand;
                var descriptor = arrayAllocationDescriptor;
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