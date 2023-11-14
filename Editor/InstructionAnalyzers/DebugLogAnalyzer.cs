using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.Modules;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    class DebugLogAnalyzer : ICodeModuleInstructionAnalyzer
    {
        static readonly int k_ModuleHashCode = "UnityEngine.CoreModule.dll".GetHashCode();
        static readonly int k_TypeHashCode = "UnityEngine.Debug".GetHashCode();
        static readonly int k_ConditionalAttributeHashCode = "System.Diagnostics.ConditionalAttribute".GetHashCode();

        internal const string PAC0192 = nameof(PAC0192);
        internal const string PAC0193 = nameof(PAC0193);

        static readonly Descriptor k_DebugLogIssueDescriptor = new Descriptor
            (
            PAC0192,
            "Debug.Log / Debug.LogFormat",
            Area.CPU,
            "<b>Debug.Log</b> methods take a lot of CPU time, especially if used frequently.",
            "Remove logging code, or strip it from release builds by using scripting symbols for conditional compilation (#if ... #endif) or the <b>ConditionalAttribute</b> on a custom logging method that calls Debug.Log. Where logging is required in release builds, CPU times can be reduced by disabling stack traces in log messages. You can do this by setting <b>Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None)</b>."
            )
        {
            documentationUrl = "https://docs.unity3d.com/Manual/BestPracticeUnderstandingPerformanceInUnity7.html",
            messageFormat = "Use of Debug.{0} in '{1}'",
            defaultSeverity = Severity.Minor
        };

        static readonly Descriptor k_DebugLogWarningIssueDescriptor = new Descriptor
            (
            PAC0193,
            "Debug.LogWarning / Debug.LogWarningFormat",
            Area.CPU,
            "<b>Debug.LogWarning</b> methods take a lot of CPU time, especially if used frequently.",
            "Remove logging code, or strip it from release builds by using scripting symbols for conditional compilation (#if ... #endif) or the <b>ConditionalAttribute</b> on a custom logging method that calls Debug.LogWarning. Where logging is required in release builds, CPU times can be reduced by disabling stack traces in log messages. You can do this by setting <b>Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None)</b>."
            )
        {
            documentationUrl = "https://docs.unity3d.com/Manual/BestPracticeUnderstandingPerformanceInUnity7.html",
            messageFormat = "Use of Debug.{0} in '{1}'",
            defaultSeverity = Severity.Minor
        };

        readonly OpCode[] m_OpCodes =
        {
            OpCodes.Call,
            OpCodes.Callvirt
        };

        public IReadOnlyCollection<OpCode> opCodes => m_OpCodes;

        public void Initialize(Module module)
        {
            module.RegisterDescriptor(k_DebugLogIssueDescriptor);
            module.RegisterDescriptor(k_DebugLogWarningIssueDescriptor);
        }

        public IssueBuilder Analyze(InstructionAnalysisContext context)
        {
            var callee = (MethodReference)context.Instruction.Operand;
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
            if (context.MethodDefinition.HasCustomAttributes && context.MethodDefinition.CustomAttributes.Any(a =>
                a.AttributeType.FullName.GetHashCode() == k_ConditionalAttributeHashCode))
            {
                return null;
            }

            switch (methodName)
            {
                case "Log":
                case "LogFormat":
                    return context.Create(IssueCategory.Code, k_DebugLogIssueDescriptor.id, methodName, context.MethodDefinition.Name);
                case "LogWarning":
                case "LogWarningFormat":
                    return context.Create(IssueCategory.Code, k_DebugLogWarningIssueDescriptor.id, methodName, context.MethodDefinition.Name);
                default:
                    return null;
            }
        }
    }
}
