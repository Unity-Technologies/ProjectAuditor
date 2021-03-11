using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.CodeAnalysis;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    public class GenericTypeInstantiationAnalyzer : IInstructionAnalyzer
    {
        const int k_FirstDescriptorId = 500000;

        Dictionary<string, ProblemDescriptor> m_GenericDescriptors = new Dictionary<string, ProblemDescriptor>();

        public void Initialize(IAuditor auditor)
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

            var typeDefinition = typeReference.Resolve();
            var genericTypeName = typeDefinition.FullName;
            if (!m_GenericDescriptors.ContainsKey(genericTypeName))
            {
                var desc = new ProblemDescriptor(k_FirstDescriptorId + m_GenericDescriptors.Count, typeDefinition.FullName, Area.BuildSize);
                m_GenericDescriptors.Add(typeDefinition.FullName, desc);
            }
            return new ProjectIssue(m_GenericDescriptors[genericTypeName], typeReference.FullName, IssueCategory.Generics, new CallTreeNode(methodDefinition));
        }

        public IEnumerable<OpCode> GetOpCodes()
        {
            yield return OpCodes.Newobj;
            yield return OpCodes.Newarr;
        }
    }
}
