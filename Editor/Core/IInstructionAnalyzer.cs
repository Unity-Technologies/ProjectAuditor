using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Unity.ProjectAuditor.Editor.Core
{
    public interface IInstructionAnalyzer
    {
        IReadOnlyCollection<OpCode> opCodes { get; }

        void Initialize(ProjectAuditorModule module);

        IssueBuilder Analyze(MethodDefinition methodDefinition, Instruction inst);
    }
}
