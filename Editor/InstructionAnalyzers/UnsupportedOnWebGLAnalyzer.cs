using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Interfaces;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    internal class UnsupportedOnWebGLAnalyzer : ICodeModuleInstructionAnalyzer
    {
        internal const string PAC1005 = nameof(PAC1005);
        internal const string PAC1006 = nameof(PAC1006);
        internal const string PAC0233 = nameof(PAC0233);

        internal static readonly Descriptor k_DescriptorSystemNet = new Descriptor
            (
            PAC1005,
            "System.Net",
            Area.Support,
            "<b>System.Net</b> is not supported on this platform. This might lead to build/runtime errors.",
            "Do not use the System.Net API on this platform."
            )
        {
            messageFormat = "'{0}' usage",
            platforms = new[] { "WebGL" }
        };

        internal static readonly Descriptor k_DescriptorSystemThreading = new Descriptor
            (
            PAC1006,
            "System.Threading",
            Area.Support,
            "Dot Net threads are not supported on this platform. Using System.Threading might lead to build/runtime errors.",
            "Do not use the <b>System.Threading</b> API on this platform."
            )
        {
            messageFormat = "'{0}' usage",
            platforms = new[] { "WebGL" }
        };

        internal static readonly Descriptor k_DescriptorMicrophone = new Descriptor
            (
            PAC0233,
            "UnityEngine.Microphone",
            Area.Support,
            "The <b>UnityEngine.Microphone</b> API is not supported on this platform. Using Microphone might lead to build/runtime errors.",
            "Do not use the Microphone API on this platform."
            )
        {
            messageFormat = "'{0}' usage",
            platforms = new[] { "WebGL" }
        };

        readonly OpCode[] m_OpCodes =
        {
            OpCodes.Call,
            OpCodes.Callvirt
        };

        public IReadOnlyCollection<OpCode> opCodes => m_OpCodes;

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_DescriptorSystemNet);
            module.RegisterDescriptor(k_DescriptorSystemThreading);
            module.RegisterDescriptor(k_DescriptorMicrophone);
        }

        public IssueBuilder Analyze(MethodDefinition methodDefinition, Instruction inst)
        {
            var methodReference = (MethodReference)inst.Operand;
            if (k_DescriptorSystemNet.IsPlatformCompatible(projectAuditorParams.platform) && methodReference.DeclaringType.FullName.StartsWith("System.Net."))
            {
                return ProjectIssue.Create(IssueCategory.Code, k_DescriptorSystemNet.id, methodReference.FullName);
            }
            if (k_DescriptorSystemThreading.IsPlatformCompatible(projectAuditorParams.platform) && methodReference.DeclaringType.FullName.StartsWith("System.Threading."))
            {
                return ProjectIssue.Create(IssueCategory.Code, k_DescriptorSystemThreading.id, methodReference.FullName);
            }
            if (k_DescriptorMicrophone.IsPlatformCompatible(projectAuditorParams.platform) && methodReference.DeclaringType.FullName.Equals("UnityEngine.Microphone"))
            {
                return ProjectIssue.Create(IssueCategory.Code, k_DescriptorMicrophone.id, methodReference.FullName);
            }

            return null;
        }
    }
}
