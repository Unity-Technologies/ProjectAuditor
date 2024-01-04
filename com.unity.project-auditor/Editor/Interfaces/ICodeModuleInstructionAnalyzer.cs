using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    internal class InstructionAnalysisContext : AnalysisContext
    {
        public MethodDefinition MethodDefinition;
        public Instruction Instruction;
    }

    internal interface ICodeModuleInstructionAnalyzer : IModuleAnalyzer
    {
        IReadOnlyCollection<OpCode> opCodes { get; }

        IssueBuilder Analyze(InstructionAnalysisContext context);
    }
}
