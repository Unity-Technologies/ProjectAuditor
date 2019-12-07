
using Mono.Cecil.Cil;

namespace Unity.ProjectAuditor.Editor
{
    [ScriptAnalyzer]
    public class BoxingAnalyzer
    {
        public static readonly ProblemDescriptor descriptor = new ProblemDescriptor
        {
            id = 102000,
            opcode = OpCodes.Box.Code.ToString(),
            type = string.Empty,
            method = string.Empty,
            area = "Memory",
            problem = "Boxing happens where a value type, such as an integer, is converted into an object of reference type. This causes an allocation on the heap, which might increase the size of the managed heap and the frequency of Garbage Collection.",
            solution = "Try to avoid Boxing when possible."
        };

        public BoxingAnalyzer(ScriptAuditor auditor)
        {
            auditor.RegisterDescriptor(descriptor);
        }
    }
}