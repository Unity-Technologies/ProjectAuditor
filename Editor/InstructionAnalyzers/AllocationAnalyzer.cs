using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.CodeAnalysis;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    class AllocationAnalyzer : IInstructionAnalyzer
    {
        static readonly ProblemDescriptor k_ObjectAllocationDescriptor = new ProblemDescriptor
            (
            "PAC2002",
            "Object Allocation",
            Area.Memory,
            "An object is allocated in managed memory",
            "Try to avoid allocating objects in frequently-updated code."
            )
        {
            messageFormat = "'{0}' allocation"
        };

        static readonly ProblemDescriptor k_ClosureAllocationDescriptor = new ProblemDescriptor
            (
            "PAC2003",
            "Closure Allocation",
            Area.Memory,
            "An object is allocated in managed memory",
            "Try to avoid allocating objects in frequently-updated code."
            )
        {
            messageFormat = "Closure allocation in '{0}.{1}'"
        };

        static readonly ProblemDescriptor k_ArrayAllocationDescriptor = new ProblemDescriptor
            (
            "PAC2004",
            "Array Allocation",
            Area.Memory,
            "An array is allocated in managed memory",
            "Try to avoid allocating arrays in frequently-updated code."
            )
        {
            messageFormat = "'{0}' array allocation"
        };

        static readonly ProblemDescriptor k_ParamArrayAllocationDescriptor = new ProblemDescriptor
            (
            "PAC2005",
            "Param Object Allocation",
            Area.Memory,
            "A parameters array is allocated.",
            "Try to avoid calling this method in frequently-updated code."
            )
        {
            messageFormat = "Parameters array '{0} {1}' allocation"
        };

        static readonly int k_ParamArrayAtributeHashCode = "System.ParamArrayAttribute".GetHashCode();

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_ObjectAllocationDescriptor);
            module.RegisterDescriptor(k_ClosureAllocationDescriptor);
            module.RegisterDescriptor(k_ArrayAllocationDescriptor);
            module.RegisterDescriptor(k_ParamArrayAllocationDescriptor);
        }

        public ProjectIssueBuilder Analyze(MethodDefinition callerMethodDefinition, Instruction inst)
        {
            if (inst.OpCode == OpCodes.Call || inst.OpCode == OpCodes.Callvirt)
            {
                var callee = (MethodReference)inst.Operand;
                if (callee.HasParameters)
                {
                    var lastParam = callee.Parameters.Last();
                    if (lastParam.HasCustomAttributes && lastParam.CustomAttributes.Any(a => a.AttributeType.FullName.GetHashCode() == k_ParamArrayAtributeHashCode))
                    {
                        return ProjectIssue.Create(IssueCategory.Code, k_ParamArrayAllocationDescriptor, lastParam.ParameterType.Name, lastParam.Name);
                    }
                }
                return null;
            }

            if (inst.OpCode == OpCodes.Newobj)
            {
                var methodReference = (MethodReference)inst.Operand;
                var typeReference = methodReference.DeclaringType;
                if (typeReference.IsValueType)
                    return null;

                var isClosure = typeReference.Name.StartsWith("<>c__DisplayClass");
                if (isClosure)
                {
                    return ProjectIssue.Create(IssueCategory.Code, k_ClosureAllocationDescriptor, callerMethodDefinition.DeclaringType.Name, callerMethodDefinition.Name);
                }
                else
                {
                    return ProjectIssue.Create(IssueCategory.Code, k_ObjectAllocationDescriptor, typeReference.FullName);
                }
            }
            else // OpCodes.Newarr
            {
                var typeReference = (TypeReference)inst.Operand;

                return ProjectIssue.Create(IssueCategory.Code, k_ArrayAllocationDescriptor, typeReference.Name);
            }
        }

        public IEnumerable<OpCode> GetOpCodes()
        {
            yield return OpCodes.Call;
            yield return OpCodes.Callvirt;
            yield return OpCodes.Newobj;
            yield return OpCodes.Newarr;
        }
    }
}
