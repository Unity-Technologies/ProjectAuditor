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

    internal abstract class CodeModuleInstructionAnalyzer : ModuleAnalyzer
    {
        public abstract IReadOnlyCollection<OpCode> opCodes { get; }

        public abstract IssueBuilder Analyze(InstructionAnalysisContext context);
    }
}
