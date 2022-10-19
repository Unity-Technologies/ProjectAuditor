using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Modules;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    class AllocationAnalyzer : ICodeModuleInstructionAnalyzer
    {
        static readonly Descriptor k_ObjectAllocationDescriptor = new Descriptor
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

        static readonly Descriptor k_ClosureAllocationDescriptor = new Descriptor
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

        static readonly Descriptor k_ArrayAllocationDescriptor = new Descriptor
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

        static readonly Descriptor k_ParamArrayAllocationDescriptor = new Descriptor
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

        readonly OpCode[] m_OpCodes =
        {
            OpCodes.Call,
            OpCodes.Callvirt,
            OpCodes.Newobj,
            OpCodes.Newarr
        };

        public IReadOnlyCollection<OpCode> opCodes => m_OpCodes;

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_ObjectAllocationDescriptor);
            module.RegisterDescriptor(k_ClosureAllocationDescriptor);
            module.RegisterDescriptor(k_ArrayAllocationDescriptor);
            module.RegisterDescriptor(k_ParamArrayAllocationDescriptor);
        }

        public IssueBuilder Analyze(MethodDefinition callerMethodDefinition, Instruction inst)
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
    }
}
