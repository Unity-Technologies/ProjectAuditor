using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace Unity.ProjectAuditor.Editor
{
    public interface IInstructionAnalyzer
    {
        ProjectIssue Analyze(Instruction inst);

        IEnumerable<OpCode> GetOpCodes();
    }
}
