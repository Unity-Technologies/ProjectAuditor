using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
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
        public bool showSeverityFilters;
        public bool showInfoPanel;
        public bool showMuteOptions;
        public bool showRightPanels;
        public GUIContent dependencyViewGuiContent;
        public Func<ProjectIssue, string> getAssemblyName;
        public Action<GenericMenu, ViewManager, ProjectIssue> onContextMenu;
        public Action<Location> onOpenIssue;
        public Action<ViewManager> onDrawToolbarDataOptions;
        public Action<ProblemDescriptor> onOpenManual;
        public int analyticsEvent;

        static readonly Dictionary<int, ViewDescriptor> s_ViewDescriptorsRegistry = new Dictionary<int, ViewDescriptor>();

        public static void Register(ViewDescriptor descriptor)
        {
            if (!s_ViewDescriptorsRegistry.ContainsKey((int)descriptor.category))
                s_ViewDescriptorsRegistry.Add((int)descriptor.category, descriptor);
        }

        public static ViewDescriptor[] GetAll()
        {
            return s_ViewDescriptorsRegistry.Select(pair => pair.Value).ToArray();
        }
    }
}
