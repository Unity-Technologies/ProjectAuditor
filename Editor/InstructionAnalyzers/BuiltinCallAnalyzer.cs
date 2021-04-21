using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    class BuiltinCallAnalyzer : IInstructionAnalyzer
    {
        Dictionary<string, List<ProblemDescriptor>> m_Descriptors; // method name as key, list of type names as value
        Dictionary<string, ProblemDescriptor> m_WholeNamespaceDescriptors; // namespace as key

        public void Initialize(IAuditor auditor)
        {
            var descriptors = ProblemDescriptorLoader.LoadFromJson(ProjectAuditor.DataPath, "ApiDatabase");
            foreach (var descriptor in descriptors)
            {
                auditor.RegisterDescriptor(descriptor);
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

            m_WholeNamespaceDescriptors = auditor.GetDescriptors().Where(descriptor => descriptor.method.Equals("*")).ToDictionary(d => d.type);
        }

        public ProjectIssue Analyze(MethodDefinition methodDefinition, Instruction inst)
        {
            var callee = (MethodReference)inst.Operand;

            // replace root with callee node
            var calleeNode = new CallTreeNode(callee);
            var description = string.Empty;

            ProblemDescriptor descriptor;
            var methodName = callee.Name;
            if (methodName.StartsWith("get_"))
                methodName = methodName.Substring("get_".Length);

            // Are we trying to warn about a whole namespace?
            m_WholeNamespaceDescriptors.TryGetValue(callee.DeclaringType.Namespace, out descriptor);
            if (descriptor != null)
            {
                description = calleeNode.prettyName;
            }
            else
            {
                List<ProblemDescriptor> descriptors;
                if (!m_Descriptors.TryGetValue(methodName, out descriptors))
                    return null;

                descriptor = descriptors.Find(d => IsOrInheritedFrom(callee.DeclaringType, d.type));
                if (descriptor == null)
                    return null;

                // by default use descriptor issue description
                description = descriptor.description;
            }

            return new ProjectIssue
            (
                descriptor,
                description,
                IssueCategory.Code,
                calleeNode
            );
        }

        public IEnumerable<OpCode> GetOpCodes()
        {
            yield return OpCodes.Call;
            yield return OpCodes.Callvirt;
        }

        static bool IsOrInheritedFrom(TypeReference typeReference, string typeName)
        {
            try
            {
                var typeDefinition = typeReference.Resolve();

                if (typeDefinition.FullName.Equals(typeName))
                    return true;

                if (typeDefinition.BaseType != null)
                    return IsOrInheritedFrom(typeDefinition.BaseType, typeName);
            }
            catch (AssemblyResolutionException e)
            {
                Debug.LogWarning(e);
            }

            return false;
        }
    }
}
