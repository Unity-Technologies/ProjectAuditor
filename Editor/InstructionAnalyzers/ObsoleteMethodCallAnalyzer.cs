using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.Auditors;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    public class ObsoleteMethodCallAnalyzer : IInstructionAnalyzer
    {
        static readonly ProblemDescriptor k_ObsoleteMethodCallDescriptor = new ProblemDescriptor
        (
            102005,
            "Obsolete method call",
            Area.Info,
            "This method is marked as obsolete",
            "Do not call this method if possible"
        );

        public void Initialize(ProjectAuditorModule module)
        {
        }

        public IEnumerable<OpCode> GetOpCodes()
        {
            yield return OpCodes.Call;
            yield return OpCodes.Callvirt;
        }

        public ProjectIssue Analyze(MethodDefinition methodDefinition, Instruction inst)
        {
            var calleeReference = (MethodReference)inst.Operand;
            try
            {
                var calleeDefinition = calleeReference.Resolve();
                var attr = MonoCecilHelper.GetCustomAttribute<ObsoleteAttribute>(calleeDefinition);
                if (attr != null)
                {
                    var desc = k_ObsoleteMethodCallDescriptor;
                    var description = string.Format("Call to '{0}' obsolete method", calleeDefinition.Name);
                    var isError = false;

                    if (attr.HasConstructorArguments && attr.ConstructorArguments.Count > 0)
                    {
                        var stringArguments = attr.ConstructorArguments.Where(a => a.Value is string).ToArray();
                        if (stringArguments.Length > 0)
                        {
                            desc = new ProblemDescriptor
                            (
                                k_ObsoleteMethodCallDescriptor.id,
                                k_ObsoleteMethodCallDescriptor.description,
                                k_ObsoleteMethodCallDescriptor.GetAreas(),
                                k_ObsoleteMethodCallDescriptor.problem,
                                stringArguments[0].Value as string
                            );
                        }
                        var boolArguments = attr.ConstructorArguments.Where(a => a.Value is bool).ToArray();
                        isError = (boolArguments.Length > 0) && (bool)boolArguments[0].Value;
                    }

                    var projectIssue = new ProjectIssue
                    (
                        desc,
                        description,
                        IssueCategory.Code
                    )
                    {
                        severity = isError ? Rule.Severity.Error : Rule.Severity.Warning
                    };

                    return projectIssue;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }
            return null;
        }
    }
}
