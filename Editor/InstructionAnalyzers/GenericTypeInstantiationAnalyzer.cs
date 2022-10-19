using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Core;
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

        public void Initialize(ProjectAuditorModule module)
        {
        }

        public IssueBuilder Analyze(MethodDefinition methodDefinition, Instruction inst)
        {
            return AnalyzeType(inst.OpCode == OpCodes.Newobj ? ((MethodReference)inst.Operand).DeclaringType : (TypeReference)inst.Operand);
        }

        IssueBuilder AnalyzeType(TypeReference typeReference)
        {
            if (!typeReference.IsGenericInstance)
                return null;

            return ProjectIssue.Create(IssueCategory.GenericInstance, $"'{typeReference.FullName}' generic instance");
        }
    }
}
