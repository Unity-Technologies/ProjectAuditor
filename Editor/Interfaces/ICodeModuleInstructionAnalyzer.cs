using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    internal interface ICodeModuleInstructionAnalyzer : IModuleAnalyzer
    {
        IReadOnlyCollection<OpCode> opCodes { get; }

        IssueBuilder Analyze(MethodDefinition methodDefinition, Instruction inst);
    }
}
