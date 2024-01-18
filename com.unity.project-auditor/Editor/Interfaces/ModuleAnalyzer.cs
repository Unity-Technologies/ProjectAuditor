using System.Reflection;
using Module = Unity.ProjectAuditor.Editor.Core.Module;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    // stephenm TODO: Make this public, for extensibility. And, I guess, move it to API. Phase 2.
    internal class ModuleAnalyzer
    {
        public virtual void Initialize(Module module)
        {
        }

        public void CacheParameters(DiagnosticParams diagnosticParams)
        {
            foreach (var field in this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var attribute = field.GetCustomAttribute<DiagnosticParameterAttribute>();
                if (attribute != null)
                {
                    field.SetValue(this, diagnosticParams.GetParameter(attribute.Name));
                }
            }
        }

        public void RegisterParameters(DiagnosticParams diagnosticParams)
        {
            foreach (var field in this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var attribute = field.GetCustomAttribute<DiagnosticParameterAttribute>();
                if (attribute != null)
                {
                    diagnosticParams.RegisterParameter(attribute.Name, attribute.DefaultValue);
                }
            }
        }
    }
}
