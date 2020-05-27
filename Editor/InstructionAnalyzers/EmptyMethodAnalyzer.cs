using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.Auditors;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    internal class EmptyMethodAnalyzer : IInstructionAnalyzer
    {
        private static readonly ProblemDescriptor s_Descriptor = new ProblemDescriptor
            (
            102001,
            "Empty MonoBehaviour Method",
            Area.CPU,
            "Any empty MonoBehaviour magic method will be included in the build and executed anyway.",
            "Remove any empty MonoBehaviour methods."
            );

        public void Initialize(IAuditor auditor)
        {
            auditor.RegisterDescriptor(s_Descriptor);
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
                s_Descriptor,
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
            return s_Descriptor;
        }
    }
}
