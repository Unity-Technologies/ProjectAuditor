using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Modules;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    class BoxingAnalyzer : ICodeModuleInstructionAnalyzer
    {
        internal const string PAC2000 = nameof(PAC2000);

        static readonly Descriptor k_Descriptor = new Descriptor
            (
            PAC2000,
            "Boxing Allocation",
            Area.Memory,
            "Boxing happens where a value type, such as an integer, is converted into an object of reference type. This causes an allocation on the heap, which might increase the size of the managed heap and the frequency of Garbage Collection.",
            "Try to avoid Boxing when possible."
            )
        {
            documentationUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/types/boxing-and-unboxing",
            messageFormat = "Conversion from value type '{0}' to ref type"
        };

        readonly OpCode[] m_OpCodes =
        {
            OpCodes.Box
        };

        public IReadOnlyCollection<OpCode> opCodes => m_OpCodes;

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_Descriptor);
        }

        public IssueBuilder Analyze(MethodDefinition methodDefinition, Instruction inst)
        {
            var type = (TypeReference)inst.Operand;
            if (type.IsGenericParameter)
            {
                var isValueType = true; // assume it's value type
                var genericType = (GenericParameter)type;
                if (genericType.HasReferenceTypeConstraint)
                    isValueType = false;
                else
#if UNITY_MONO_CECIL_1_11_4_OR_NEWER
                    foreach (var constraint in genericType.Constraints)
                        if (!constraint.ConstraintType.IsValueType)
                            isValueType = false;
#else
                    foreach (var constraint in genericType.Constraints)
                        if (!constraint.IsValueType)
                            isValueType = false;
#endif
                if (!isValueType)
                    // boxing on ref types are no-ops, so not a problem
                    return null;
            }

            var typeName = type.Name;
            if (type.FullName.Equals("System.Single"))
                typeName = "float";
            else if (type.FullName.Equals("System.Double"))
                typeName = "double";

            return ProjectIssue.Create(IssueCategory.Code, k_Descriptor, typeName);
        }
    }
}
