using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.Auditors;
using Unity.ProjectAuditor.Editor.CallAnalysis;
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

        public static ProblemDescriptor GetDescriptor()
        {
            return descriptor;
        }

        private string[] m_MonoBehaviourMagicMethods = new[]
            {"Awake", "Start", "OnEnable", "OnDisable", "Update", "LateUpdate", "OnEnable", "FixedUpdate"};
        
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

            if (!m_MonoBehaviourMagicMethods.Contains(methodDefinition.Name))
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
    }
}