using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    class BuiltinCallAnalyzer : IInstructionAnalyzer
    {
        Dictionary<string, List<ProblemDescriptor>> m_Descriptors; // method name as key, list of type names as value
        Dictionary<string, ProblemDescriptor> m_WholeNamespaceDescriptors; // namespace as key

        public void Initialize(ProjectAuditorModule module)
        {
            var descriptors = ProblemDescriptorLoader.LoadFromJson(ProjectAuditor.DataPath, "ApiDatabase");
            foreach (var descriptor in descriptors)
            {
                module.RegisterDescriptor(descriptor);
            }

            var methodDescriptors = descriptors.Where(descriptor => !descriptor.method.Equals("*") && !string.IsNullOrEmpty(descriptor.type));

            m_Descriptors = new Dictionary<string, List<ProblemDescriptor>>();
            foreach (var d in methodDescriptors)
            {
                if (!m_Descriptors.ContainsKey(d.method))
                {
                    m_Descriptors.Add(d.method, new List<ProblemDescriptor>());
                }
                m_Descriptors[d.method].Add(d);
            }

            m_WholeNamespaceDescriptors = module.GetDescriptors().Where(descriptor => descriptor.method.Equals("*")).ToDictionary(d => d.type);
        }

        public ProjectIssueBuilder Analyze(MethodDefinition methodDefinition, Instruction inst)
        {
            var callee = (MethodReference)inst.Operand;
            var description = string.Empty;
            var methodName = callee.Name;

            ProblemDescriptor descriptor;
            var declaringType = callee.DeclaringType;

            // Are we trying to warn about a whole namespace?
            if (m_WholeNamespaceDescriptors.TryGetValue(declaringType.Namespace, out descriptor))
            {
                description = string.Format("'{0}.{1}' usage", declaringType, methodName);
            }
            else
            {
                if (methodName.StartsWith("get_"))
                    methodName = methodName.Substring("get_".Length);

                List<ProblemDescriptor> descriptors;
                if (!m_Descriptors.TryGetValue(methodName, out descriptors))
                    return null;

                Profiler.BeginSample("BuiltinCallAnalyzer.FindDescriptor");
                descriptor = descriptors.Find(d => MonoCecilHelper.IsOrInheritedFrom(declaringType, d.type));
                Profiler.EndSample();

                if (descriptor == null)
                    return null;

                // by default use descriptor issue description
                description = string.Format("'{0}' usage", descriptor.description);

                var genericInstanceMethod = callee as GenericInstanceMethod;
                if (genericInstanceMethod != null && genericInstanceMethod.HasGenericArguments)
                {
                    var genericTypeNames = genericInstanceMethod.GenericArguments.Select(a => a.FullName).ToArray();
                    description = string.Format("'{0}' usage (with generic argument '{1}')", descriptor.description, string.Join(", ", genericTypeNames));
                }
            }

            return ProjectIssue.Create(IssueCategory.Code, descriptor)
                .WithDescription(description);
        }

        public IEnumerable<OpCode> GetOpCodes()
        {
            yield return OpCodes.Call;
            yield return OpCodes.Callvirt;
        }
    }
}
