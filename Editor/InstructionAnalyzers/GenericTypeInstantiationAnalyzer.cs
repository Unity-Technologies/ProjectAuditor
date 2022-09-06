using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Core;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    class GenericTypeInstantiationAnalyzer : IInstructionAnalyzer
    {
        public void Initialize(ProjectAuditorModule module)
        {
        }

        public ProjectIssueBuilder Analyze(MethodDefinition methodDefinition, Instruction inst)
        {
            return AnalyzeType(inst.OpCode == OpCodes.Newobj ? ((MethodReference)inst.Operand).DeclaringType : (TypeReference)inst.Operand);
        }

        ProjectIssueBuilder AnalyzeType(TypeReference typeReference)
        {
            if (!typeReference.IsGenericInstance)
                return null;

            return ProjectIssue.Create(IssueCategory.GenericInstance, $"'{typeReference.FullName}' generic instance");
        }

        public IEnumerable<OpCode> GetOpCodes()
        {
            yield return OpCodes.Newobj;
            yield return OpCodes.Newarr;
        }
    }
}
