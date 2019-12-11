
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Unity.ProjectAuditor.Editor
{
    [ScriptAnalyzer]
    public class BoxingAnalyzer : IInstructionAnalyzer
    {
        private static readonly ProblemDescriptor descriptor = new ProblemDescriptor
        {
            id = 102000,
            opcode = OpCodes.Box.Code.ToString(),
            type = string.Empty,
            method = string.Empty,
            area = "Memory",
            problem = "Boxing happens where a value type, such as an integer, is converted into an object of reference type. This causes an allocation on the heap, which might increase the size of the managed heap and the frequency of Garbage Collection.",
            solution = "Try to avoid Boxing when possible."
        };

        private OpCode[] m_OpCodes = new[] {OpCodes.Box};
        
        public BoxingAnalyzer(ScriptAuditor auditor)
        {
            auditor.RegisterDescriptor(descriptor);
        }
        
        public ProjectIssue Analyze(Instruction inst)
        {
            var type = (TypeReference) inst.Operand;
            if (type.IsGenericParameter)
            {
                bool isValueType = true; // assume it's value type
                var genericType = (GenericParameter) type;
                if (genericType.HasReferenceTypeConstraint)
                {
                    isValueType = false;
                }
                else
                {
                    foreach (var constraint in genericType.Constraints)
                    {
                        if (!constraint.IsValueType)
                            isValueType = false;
                    }
                }

                if (!isValueType)
                {
                    // boxing on ref types are no-ops, so not a problem
                    return null;
                }
            }

            var description = string.Format("Conversion from value type '{0}' to ref type", type.Name);
            var calleeNode = new CallTreeNode(inst.OpCode.Code.ToString());
            
            return new ProjectIssue
            {
                description = description,
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