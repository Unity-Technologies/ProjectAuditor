using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.Modules
{
    public interface ICodeModuleInstructionAnalyzer
    {
        IReadOnlyCollection<OpCode> opCodes { get; }

        void Initialize(ProjectAuditorModule module);

        IssueBuilder Analyze(MethodDefinition methodDefinition, Instruction inst);
    }
}
