using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Modules;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    class DebugLogAnalyzer : ICodeModuleInstructionAnalyzer
    {
        static readonly int k_ModuleHashCode = "UnityEngine.CoreModule.dll".GetHashCode();
        static readonly int k_TypeHashCode = "UnityEngine.Debug".GetHashCode();
        static readonly int k_ConditionalAttributeHashCode = "System.Diagnostics.ConditionalAttribute".GetHashCode();

        static readonly Descriptor k_DebugLogIssueDescriptor = new Descriptor
            (
            "PAC0192",
            "Debug.Log / Debug.LogFormat",
            Area.CPU,
            "<b>Debug.Log</b> methods cause slowdowns, especially if used frequently.",
            "Instead of removing code an option is to strip this code on release builds by using scripting symbols for conditional compilation (#if ... #endif) or the <b>ConditionalAttribute</b> on a method where you call this. When logging is still used in your code a small optimization can be to leave out the callstack, if not required, by setting <b>Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None)</b> via code."
            )
        {
            messageFormat = "Use of Debug.{0} in '{1}'"
        };

        static readonly Descriptor k_DebugLogWarningIssueDescriptor = new Descriptor
            (
            "PAC0193",
            "Debug.LogWarning / Debug.LogWarningFormat",
            Area.CPU,
            "<b>Debug.LogWarning</b> methods cause slowdowns, especially if used frequently.",
            "Instead of removing code an option is to strip this code on release builds by using scripting symbols for conditional compilation (#if ... #endif) or the <b>ConditionalAttribute</b> on a method where you call this. When logging is still used in your code a small optimization can be to leave out the callstack, if not required, by setting <b>Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None)</b> via code."
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
            module.RegisterDescriptor(k_DebugLogWarningIssueDescriptor);
        }

        public IssueBuilder Analyze(MethodDefinition methodDefinition, Instruction inst)
        {
            var callee = (MethodReference)inst.Operand;
            var methodName = callee.Name;
            var declaringType = callee.DeclaringType;

            if (k_TypeHashCode != declaringType.FullName.GetHashCode())
                return null;

            // second check on module name which requires resolving the type
            try
            {
                var typeDefinition = declaringType.Resolve();
                if (typeDefinition == null)
                {
                    Debug.LogWarning(declaringType.FullName + " could not be resolved.");
                    return null;
                }

                if (k_ModuleHashCode != typeDefinition.Module.Name.GetHashCode())
                    return null;
            }
            catch (AssemblyResolutionException e)
            {
                Debug.LogWarningFormat("Could not resolve {0}: {1}", declaringType.Name, e.Message);
            }

            // If we find the ConditionalAttribute, we assume this is intended to be compiled out on release
            if (methodDefinition.HasCustomAttributes && methodDefinition.CustomAttributes.Any(a =>
                a.AttributeType.FullName.GetHashCode() == k_ConditionalAttributeHashCode))
            {
                return null;
            }

            switch (methodName)
            {
                case "Log":
                case "LogFormat":
                    return ProjectIssue.Create(IssueCategory.Code, k_DebugLogIssueDescriptor, methodName, methodDefinition.Name);
                case "LogWarning":
                case "LogWarningFormat":
                    return ProjectIssue.Create(IssueCategory.Code, k_DebugLogWarningIssueDescriptor, methodName, methodDefinition.Name);
                default:
                    return null;
            }
        }
    }
}
