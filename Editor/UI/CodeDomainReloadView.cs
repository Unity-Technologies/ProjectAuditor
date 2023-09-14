using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    internal class CodeDomainReloadView : CodeDiagnosticView
    {
        public CodeDomainReloadView(ViewManager viewManager) : base(viewManager)
        {
        }

        public override void DrawFilters()
        {
        }
    }
}
