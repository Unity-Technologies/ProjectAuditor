using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.Modules;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    class GenericTypeInstantiationAnalyzer : ICodeModuleInstructionAnalyzer
    {
        readonly OpCode[] m_OpCodes =
        {
            OpCodes.Newobj,
            OpCodes.Newarr
        };

        public IReadOnlyCollection<OpCode> opCodes => m_OpCodes;

        public void Initialize(Module module)
        {
        }

        public IssueBuilder Analyze(InstructionAnalysisContext context)
        {
            return AnalyzeType(context, context.Instruction.OpCode == OpCodes.Newobj ? ((MethodReference)context.Instruction.Operand).DeclaringType : (TypeReference)context.Instruction.Operand);
        }

        IssueBuilder AnalyzeType(InstructionAnalysisContext context, TypeReference typeReference)
        {
            if (!typeReference.IsGenericInstance)
                return null;

            return context.CreateInsight(IssueCategory.GenericInstance, $"'{typeReference.FullName}' generic instance");
        }
    }
}
