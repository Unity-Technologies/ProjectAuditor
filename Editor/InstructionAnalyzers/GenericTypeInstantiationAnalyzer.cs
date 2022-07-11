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

        // TODO: replace with single descriptor
        readonly Dictionary<string, ProblemDescriptor> m_GenericDescriptors = new Dictionary<string, ProblemDescriptor>();

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

            try
            {
                var typeDefinition = typeReference.Resolve();
                var genericTypeName = typeDefinition.FullName;
                if (!m_GenericDescriptors.ContainsKey(genericTypeName))
                {
                    var desc = new ProblemDescriptor(k_FirstDescriptorId + m_GenericDescriptors.Count,
                        typeDefinition.FullName, Area.BuildSize)
                    {
                        messageFormat = "'{0}' generic instance"
                    };
                    m_GenericDescriptors.Add(typeDefinition.FullName, desc);
                }

                return ProjectIssue.Create(IssueCategory.GenericInstance, m_GenericDescriptors[genericTypeName], typeReference.FullName);
            }
            catch (AssemblyResolutionException e)
            {
                Debug.LogWarningFormat("Could not resolve {0}: {1}", typeReference.Name, e.Message);
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
