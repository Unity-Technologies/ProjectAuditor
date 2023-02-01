using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Modules;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    class EmptyMethodAnalyzer : ICodeModuleInstructionAnalyzer
    {
        static readonly Descriptor k_Descriptor = new Descriptor
            (
            "PAC2001",
            "Empty MonoBehaviour Method",
            Area.CPU,
            "Any empty MonoBehaviour magic method will be included in the build and executed anyway.",
            "Remove any empty MonoBehaviour methods."
            )
                .WithMessageFormat("MonoBehaviour method '{0}' is empty");

        readonly OpCode[] m_OpCodes =
        {
            OpCodes.Ret
        };

        public IReadOnlyCollection<OpCode> opCodes => m_OpCodes;

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_Descriptor);
        }

        public IssueBuilder Analyze(MethodDefinition methodDefinition, Instruction inst)
        {
            // skip any no-op
            var previousIL = inst.Previous;
            while (previousIL != null && previousIL.OpCode == OpCodes.Nop)
                previousIL = previousIL.Previous;

            // if there is no instruction before OpCodes.Ret, then we know this method is empty
            if (previousIL != null)
                return null;

            if (!MonoBehaviourAnalysis.IsMonoBehaviour(methodDefinition.DeclaringType))
                return null;

            if (!MonoBehaviourAnalysis.IsMonoBehaviourEvent(methodDefinition))
                return null;

            return ProjectIssue.Create(IssueCategory.Code, k_Descriptor, methodDefinition.Name);
        }

        public static Descriptor GetDescriptor()
        {
            return k_Descriptor;
        }
    }
}
