using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    public interface IInstructionAnalyzer
    {
        ProjectIssue Analyze(MethodDefinition methodDefinition, Instruction inst);

        IEnumerable<OpCode> GetOpCodes();
    }
}