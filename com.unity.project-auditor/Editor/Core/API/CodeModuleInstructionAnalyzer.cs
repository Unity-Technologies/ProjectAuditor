using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Unity.ProjectAuditor.Editor.Core
{
    // stephenm TODO: Document
    public class InstructionAnalysisContext : AnalysisContext
    {
        public MethodDefinition MethodDefinition;
        public Instruction Instruction;
    }

    // stephenm TODO: Document
    internal abstract class CodeModuleInstructionAnalyzer : ModuleAnalyzer
    {
        public abstract IReadOnlyCollection<OpCode> opCodes { get; }

        public abstract IssueBuilder Analyze(InstructionAnalysisContext context);
    }
}
