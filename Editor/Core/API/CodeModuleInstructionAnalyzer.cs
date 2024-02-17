using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Unity.ProjectAuditor.Editor.Core
{
    /// <summary>
    /// A context object passed by CodeModule to a CodeModuleInstructionAnalyzer's Analyze() method.
    /// </summary>
    public class InstructionAnalysisContext : AnalysisContext
    {
        /// <summary>
        /// A Mono.Cecil Method Definition containing information about the current method being analyzed.
        /// </summary>
        public MethodDefinition MethodDefinition;

        /// <summary>
        /// A Mono.Cecil Instruction containing information about the current code instruction being analyzed.
        /// </summary>
        public Instruction Instruction;
    }

    /// <summary>
    /// Abstract base class for an Analyzer to be invoked by CodeModule
    /// </summary>
    public abstract class CodeModuleInstructionAnalyzer : ModuleAnalyzer
    {
        /// <summary>
        /// A collection of Mono.Cecil OpCodes which are used by this analyzer.
        /// </summary>
        /// <remarks>
        /// To speed up the code analysis process, each CodeModuleInstructionAnalyzer must provide a list of the
        /// Instruction OpCodes it's interested in. Project Auditor will only invoke an InstructionAnalyzer if the
        /// OpCode of the Instruction currently under analysis matches one of the OpCodes in this list. For more
        /// details, see the following Mono.Cecil github page:
        /// <seealso cref="https://github.com/jbevain/cecil/blob/master/Mono.Cecil.Cil/OpCodes.cs"/>.
        /// </remarks>
        public abstract IReadOnlyCollection<OpCode> opCodes { get; }

        /// <summary>
        /// Implement this method to detect Issues in a code instruction, and construct a ReportItemBuilder object with
        /// basic information about a ReportItem object to describe the issue.
        /// </summary>
        /// <param name="context">Context object containing information necessary to perform analysis</param>
        /// <returns>A ReportItemBuilder containing a partially-constructed ReportItem</returns>
        /// <remarks>
        /// When Instruction Analyzers detect an issue, they should call <seealso cref="AnalysisContext.CreateIssue"/>
        /// to begin creating an issue with an IssueCategory, a DescriptorId and any other relevant data. The Code Module
        /// will add further information including the DependencyNode, Location and assembly name and add the resulting
        /// ReportItem to the report.
        /// </remarks>
        public abstract ReportItemBuilder Analyze(InstructionAnalysisContext context);
    }
}
