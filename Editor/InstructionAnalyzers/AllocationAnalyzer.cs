using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.Modules;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    class AllocationAnalyzer : ICodeModuleInstructionAnalyzer
    {
        internal const string PAC2002 = nameof(PAC2002);
        internal const string PAC2003 = nameof(PAC2003);
        internal const string PAC2004 = nameof(PAC2004);
        internal const string PAC2005 = nameof(PAC2005);

        static readonly Descriptor k_ObjectAllocationDescriptor = new Descriptor
            (
            PAC2002,
            "Object Allocation",
            Area.Memory,
            "An object is allocated in managed memory.",
            "Try to avoid allocating objects in frequently-updated code."
            )
        {
            messageFormat = "'{0}' allocation",
            defaultSeverity = Severity.Minor
        };

        static readonly Descriptor k_ClosureAllocationDescriptor = new Descriptor
            (
            PAC2003,
            "Closure Allocation",
            Area.Memory,
            "A closure is allocating managed memory. A closure occurs when a variable's state is captured by an in-line delegate, anonymous method or lambda which accesses that variable.",
            "Try to avoid allocating objects in frequently-updated code."
            )
        {
            messageFormat = "Closure allocation in '{0}.{1}'",
            defaultSeverity = Severity.Minor
        };

        static readonly Descriptor k_ArrayAllocationDescriptor = new Descriptor
            (
            PAC2004,
            "Array Allocation",
            Area.Memory,
            "An array is allocated in managed memory.",
            "Try to avoid allocating arrays in frequently-updated code."
            )
        {
            messageFormat = "'{0}' array allocation",
            defaultSeverity = Severity.Minor
        };

        static readonly Descriptor k_ParamArrayAllocationDescriptor = new Descriptor
            (
            PAC2005,
            "Param Object Allocation",
            Area.Memory,
            "A parameters array is allocated in managed memory.",
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

        public IssueBuilder Analyze(InstructionAnalysisContext context)
        {
            if (context.Instruction.OpCode == OpCodes.Call || context.Instruction.OpCode == OpCodes.Callvirt)
            {
                var callee = (MethodReference)context.Instruction.Operand;
                if (callee.HasParameters)
                {
                    var lastParam = callee.Parameters.Last();
                    if (lastParam.HasCustomAttributes && lastParam.CustomAttributes.Any(a => a.AttributeType.FullName.GetHashCode() == k_ParamArrayAtributeHashCode))
                    {
                        return context.Create(IssueCategory.Code, k_ParamArrayAllocationDescriptor.id, lastParam.ParameterType.Name, lastParam.Name);
                    }
                }
                return null;
            }

            if (context.Instruction.OpCode == OpCodes.Newobj)
            {
                var methodReference = (MethodReference)context.Instruction.Operand;
                var typeReference = methodReference.DeclaringType;
                if (typeReference.IsValueType)
                    return null;

                var isClosure = typeReference.Name.StartsWith("<>c__DisplayClass");
                if (isClosure)
                {
                    return context.Create(IssueCategory.Code, k_ClosureAllocationDescriptor.id, context.MethodDefinition.DeclaringType.Name, context.MethodDefinition.Name);
                }
                else
                {
                    return context.Create(IssueCategory.Code, k_ObjectAllocationDescriptor.id, typeReference.FullName);
                }
            }
            else // OpCodes.Newarr
            {
                var typeReference = (TypeReference)context.Instruction.Operand;

                return context.Create(IssueCategory.Code, k_ArrayAllocationDescriptor.id, typeReference.Name);
            }
        }
    }
}
