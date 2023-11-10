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

        bool descriptorSystemNetSupported;
        bool descriptorSystemThreadingSupported;
        bool descriptorMicrophoneSupported;

        public IReadOnlyCollection<OpCode> opCodes => m_OpCodes;

        public void Initialize(Module module)
        {
            module.RegisterDescriptor(k_DescriptorSystemNet);
            module.RegisterDescriptor(k_DescriptorSystemThreading);
            module.RegisterDescriptor(k_DescriptorMicrophone);

            descriptorSystemNetSupported = module.SupportsDescriptor(k_DescriptorSystemNet.id);
            descriptorSystemThreadingSupported = module.SupportsDescriptor(k_DescriptorSystemThreading.id);
            descriptorMicrophoneSupported = module.SupportsDescriptor(k_DescriptorMicrophone.id);
        }

        public IssueBuilder Analyze(InstructionAnalysisContext context)
        {
            var methodReference = (MethodReference)context.Instruction.Operand;
            if (descriptorSystemNetSupported && methodReference.DeclaringType.FullName.StartsWith("System.Net."))
            {
                return context.Create(IssueCategory.Code, k_DescriptorSystemNet.id, methodReference.FullName);
            }
            if (descriptorSystemThreadingSupported && methodReference.DeclaringType.FullName.StartsWith("System.Threading."))
            {
                return context.Create(IssueCategory.Code, k_DescriptorSystemThreading.id, methodReference.FullName);
            }
            if (descriptorMicrophoneSupported && methodReference.DeclaringType.FullName.Equals("UnityEngine.Microphone"))
            {
                return context.Create(IssueCategory.Code, k_DescriptorMicrophone.id, methodReference.FullName);
            }

            return null;
        }
    }
}
