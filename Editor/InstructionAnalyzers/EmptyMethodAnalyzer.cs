using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.Auditors;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    [Attribute]
    public class EmptyMethodAnalyzer : IInstructionAnalyzer
    {
        private static readonly ProblemDescriptor descriptor = new ProblemDescriptor
        {
            id = 102001,
            description = "Empty MonoBehaviour Method",
            type = string.Empty,
            method = string.Empty,
            area = "CPU",
            problem = "Any empty MonoBehaviour magic method will be included in the build and executed anyway.",
            solution = "Remove any empty MonoBehaviour methods."
        };

        public EmptyMethodAnalyzer(ScriptAuditor auditor)
        {
            auditor.RegisterDescriptor(descriptor);
        }

        public ProjectIssue Analyze(MethodDefinition methodDefinition, Instruction inst)
        {
            if (inst.Previous != null)
                return null;

            if (!MonoBehaviourAnalysis.IsMonoBehaviour(methodDefinition.DeclaringType))
                return null;

            if (!MonoBehaviourAnalysis.IsMonoBehaviourMagicMethod(methodDefinition))
                return null;

            return new ProjectIssue
            (
                descriptor,
                methodDefinition.FullName,
                IssueCategory.ApiCalls,
                new CallTreeNode(methodDefinition)
            );
        }

        public IEnumerable<OpCode> GetOpCodes()
        {
            yield return OpCodes.Ret;
        }

        public static ProblemDescriptor GetDescriptor()
        {
            return descriptor;
        }
    }
}