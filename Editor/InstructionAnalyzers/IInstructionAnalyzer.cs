using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    public interface IInstructionAnalyzer
    {
        void Initialize(ProjectAuditorModule module);

        ProjectIssue Analyze(MethodDefinition methodDefinition, Instruction inst);

        IEnumerable<OpCode> GetOpCodes();
    }
}
