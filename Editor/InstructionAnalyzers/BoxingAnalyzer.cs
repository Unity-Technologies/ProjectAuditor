using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.Auditors;
using Unity.ProjectAuditor.Editor.CodeAnalysis;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    [Attribute]
    internal class BoxingAnalyzer : IInstructionAnalyzer
    {
        private static readonly ProblemDescriptor descriptor = new ProblemDescriptor
        {
            id = 102000,
            description = "Boxing Allocation",
            type = string.Empty,
            method = string.Empty,
            area = "Memory",
            problem =
                "Boxing happens where a value type, such as an integer, is converted into an object of reference type. This causes an allocation on the heap, which might increase the size of the managed heap and the frequency of Garbage Collection.",
            solution = "Try to avoid Boxing when possible."
        };

        public BoxingAnalyzer(IAuditor auditor)
        {
            auditor.RegisterDescriptor(descriptor);
        }

        public ProjectIssue Analyze(MethodDefinition methodDefinition, Instruction inst)
        {
            var type = (TypeReference)inst.Operand;
            if (type.IsGenericParameter)
            {
                var isValueType = true; // assume it's value type
                var genericType = (GenericParameter)type;
                if (genericType.HasReferenceTypeConstraint)
                    isValueType = false;
                else
                    foreach (var constraint in genericType.Constraints)
                        if (!constraint.IsValueType)
                            isValueType = false;

                if (!isValueType)
                    // boxing on ref types are no-ops, so not a problem
                    return null;
            }

            var typeName = type.Name;
            if (type.FullName.Equals("System.Single"))
                typeName = "float";
            else if (type.FullName.Equals("System.Double"))
                typeName = "double";

            var description = string.Format("Conversion from value type '{0}' to ref type", typeName);
            var calleeNode = new CallTreeNode(methodDefinition);

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
            yield return OpCodes.Box;
        }
    }
}
