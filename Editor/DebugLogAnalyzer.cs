using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Modules;

namespace Unity.ProjectAuditor.Editor
{
    public class DebugLogAnalyzer : ICodeModuleInstructionAnalyzer
    {
        static readonly int k_ConditionalAttributeHashCode = "System.Diagnostics.ConditionalAttribute".GetHashCode();

        static readonly Descriptor k_DebugLogIssueDescriptor = new Descriptor
            (
            "PAC0192",
            "Debug.Log / Debug.LogFormat",
            Area.CPU,
            "Debug.Log methods cause slowdowns, especially if used frequently.",
            "Instead of removing code an option is to strip this code on release builds by using scripting symbols for conditional compilation (#if ... #endif) or the ConditionalAttribute on a method where you call this. When logging is still used in your code a small optimization can be to leave out the callstack, if not required, by setting 'Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None)' via code."
            )
        {
            messageFormat = "Use of Debug.{0} in '{1}'"
        };

        static readonly Descriptor k_DebugWarningIssueDescriptor = new Descriptor
            (
            "PAC0193",
            "Debug.Warning / Debug.WarningFormat",
            Area.CPU,
            "Debug.Warning methods cause slowdowns, especially if used frequently.",
            "Instead of removing code an option is to strip this code on release builds by using scripting symbols for conditional compilation (#if ... #endif) or the ConditionalAttribute on a method where you call this. When logging is still used in your code a small optimization can be to leave out the callstack, if not required, by setting 'Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None)' via code."
            )
        {
            messageFormat = "Use of Debug.{0} in '{1}'"
        };

        readonly OpCode[] m_OpCodes =
        {
            OpCodes.Call,
            OpCodes.Callvirt
        };

        public IReadOnlyCollection<OpCode> opCodes => m_OpCodes;

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_DebugLogIssueDescriptor);
            module.RegisterDescriptor(k_DebugWarningIssueDescriptor);
        }

        public IssueBuilder Analyze(MethodDefinition methodDefinition, Instruction inst)
        {
            var callee = (MethodReference)inst.Operand;
            var methodName = callee.Name;

            var declaringType = callee.DeclaringType;

            if (!MonoCecilHelper.IsOrInheritedFrom(declaringType, "UnityEngine.Debug"))
                return null;

            // If we find the ConditionalAttribute, we assume this is intended to be compiled out on release
            if (methodDefinition.HasCustomAttributes && methodDefinition.CustomAttributes.Any(a =>
                a.AttributeType.FullName.GetHashCode() == k_ConditionalAttributeHashCode))
            {
                return null;
            }

            if (methodName == "Log" || methodName == "LogFormat")
            {
                return ProjectIssue.Create(IssueCategory.Code, k_DebugLogIssueDescriptor, methodName, methodDefinition.Name);
            }

            if (methodName == "LogWarning" || methodName == "LogWarningFormat")
            {
                return ProjectIssue.Create(IssueCategory.Code, k_DebugWarningIssueDescriptor, methodName, methodDefinition.Name);
            }

            return null;
        }
    }
}
