using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    class GenericTypeInstantiationAnalyzer : IInstructionAnalyzer
    {
        const int k_FirstDescriptorId = 500000;

        Dictionary<string, ProblemDescriptor> m_GenericDescriptors = new Dictionary<string, ProblemDescriptor>();

        public void Initialize(ProjectAuditorModule module)
        {
        }

        public ProjectIssue Analyze(MethodDefinition methodDefinition, Instruction inst)
        {
            return AnalyzeType(methodDefinition, inst.OpCode == OpCodes.Newobj ? ((MethodReference)inst.Operand).DeclaringType : (TypeReference)inst.Operand);
        }

        ProjectIssue AnalyzeType(MethodDefinition methodDefinition, TypeReference typeReference)
        {
            if (!typeReference.IsGenericInstance)
                return null;

            try
            {
                var typeDefinition = typeReference.Resolve();
                var genericTypeName = typeDefinition.FullName;
                if (!m_GenericDescriptors.ContainsKey(genericTypeName))
                {
                    var desc = new ProblemDescriptor(k_FirstDescriptorId + m_GenericDescriptors.Count, typeDefinition.FullName, Area.BuildSize);
                    m_GenericDescriptors.Add(typeDefinition.FullName, desc);
                }

                return new ProjectIssue(m_GenericDescriptors[genericTypeName], typeReference.FullName, IssueCategory.GenericInstance, new CallTreeNode(methodDefinition));
            }
            catch (AssemblyResolutionException e)
            {
                Debug.LogWarning(e);
            }

            return null;
        }

        public IEnumerable<OpCode> GetOpCodes()
        {
            yield return OpCodes.Newobj;
            yield return OpCodes.Newarr;
        }
    }
}
