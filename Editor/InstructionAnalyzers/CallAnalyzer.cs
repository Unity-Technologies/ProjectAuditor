using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.Auditors;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    [Attribute]
    public class CallAnalyzer : IInstructionAnalyzer
    {
        private IEnumerable<ProblemDescriptor> m_Descriptors;

        public CallAnalyzer(ScriptAuditor auditor)
        {
            m_Descriptors = auditor.GetDescriptors();
        }
        
        public ProjectIssue Analyze(MethodDefinition methodDefinition, Instruction inst)
        {
            var callee = ((MethodReference) inst.Operand);

            // replace root with callee node
            var calleeNode = new CallTreeNode(callee);
            
            var description = string.Empty;
            var descriptor = m_Descriptors.SingleOrDefault(c => c.type == callee.DeclaringType.FullName &&
                                                                       (c.method == callee.Name ||
                                                                        ("get_" + c.method) == callee.Name));

            if (descriptor != null)
            {
                // by default use descriptor issue description
                description = descriptor.description;
            }
            else
            {
                // Are we trying to warn about a whole namespace?
                descriptor = m_Descriptors.SingleOrDefault(c =>
                    c.type == callee.DeclaringType.Namespace && c.method == "*");
                if (descriptor == null)
                {
                    // no issue found
                    return null;
                }
                
                // use callee name since it's more informative
                description = calleeNode.prettyName;
            }

            return new ProjectIssue
            (
                descriptor,
                description,
                IssueCategory.ApiCalls,
                calleeNode
            );
        }

        public IEnumerable<OpCode> GetOpCodes()
        {
            yield return OpCodes.Call;
            yield return OpCodes.Callvirt;
        }
    }
}