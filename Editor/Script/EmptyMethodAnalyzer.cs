using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.Editor
{
    [ScriptAnalyzer]
    public class EmptyMethodAnalyzer : IInstructionAnalyzer
    {
        private static readonly ProblemDescriptor descriptor = new ProblemDescriptor
        {
            id = 10201,
            description = "Empty Method",
            type = string.Empty,
            method = string.Empty,
            area = "CPU",
            problem = "If this is a MonoBehaviour class, any empty Awake/Start/Update/LateUpdate/FixedUpdate will be included in the build and executed anyway.",
            solution = "Remove any empty MonoBehaviour methods."
        };

        public static ProblemDescriptor GetDescriptor()
        {
            return descriptor;
        }
        
        private OpCode[] m_OpCodes = new[] {OpCodes.Ret};
        
        public EmptyMethodAnalyzer(ScriptAuditor auditor)
        {
            auditor.RegisterDescriptor(descriptor);
        }
        
        public ProjectIssue Analyze(MethodDefinition methodDefinition, Instruction inst)
        {
            if (inst.Previous != null)
                return null;

            if (!MonoCecilHelper.IsMonoBehaviour(methodDefinition.DeclaringType))
                return null;

            var calleeNode = new CallTreeNode(descriptor.description);
            return new ProjectIssue
            {
                description = descriptor.description,
                category = IssueCategory.ApiCalls,
                descriptor = descriptor,
                callTree = calleeNode
            };
        }

        public IEnumerable<OpCode> GetOpCodes()
        {
            return m_OpCodes;
        }
    }
}