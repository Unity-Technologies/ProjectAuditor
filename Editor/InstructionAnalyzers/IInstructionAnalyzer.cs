using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    public interface IInstructionAnalyzer
    {
        IReadOnlyCollection<OpCode> opCodes { get; }

        void Initialize(ProjectAuditorModule module);

        ProjectIssueBuilder Analyze(MethodDefinition methodDefinition, Instruction inst);
    }
}
