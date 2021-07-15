using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    public class ViewDescriptor
    {
        public Type type;
        public IssueCategory category;
        public string name;
        public string menuLabel;
        public int menuOrder;
        public bool groupByDescriptor;
        public bool descriptionWithIcon;
        public bool showActions;
        public bool showAreaSelection;
        public bool showAssemblySelection;
        public bool showCritical;
        public bool showDependencyView;
        public bool showFilters;
        public bool showInfoPanel;
        public bool showMuteOptions;
        public bool showRightPanels;
        public GUIContent dependencyViewGuiContent;
        public Action<Location> onDoubleClick;
        public Action<ViewManager> onDrawToolbarDataOptions;
        public Action<ProblemDescriptor> onOpenDescriptor;
        public int analyticsEvent;

        static Dictionary<int, ViewDescriptor> s_AnalysisViewDescriptors = new Dictionary<int, ViewDescriptor>();

        public static void Register(ViewDescriptor descriptor)
        {
            if (!s_AnalysisViewDescriptors.ContainsKey((int)descriptor.category))
                s_AnalysisViewDescriptors.Add((int)descriptor.category, descriptor);
        }

        public static ViewDescriptor[] GetAll()
        {
            return s_AnalysisViewDescriptors.Select(pair => pair.Value).ToArray();
        }
    }
}
