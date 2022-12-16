using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Diagnostic;
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
        public bool descriptionWithIcon;
        public bool showAreaSelection;
        public bool showAssemblySelection;
        public bool showDependencyView;
        public bool showFilters;
        public bool showInfoPanel;
        public GUIContent dependencyViewGuiContent;
        public Func<ProjectIssue, string> getAssemblyName;
        public Action<GenericMenu, ViewManager, ProjectIssue> onContextMenu;
        public Action<ViewManager> onDrawToolbar;
        public Action<Location> onOpenIssue;
        public Action<Descriptor> onOpenManual;
        public int analyticsEvent;

        static readonly Dictionary<int, ViewDescriptor> s_ViewDescriptorsRegistry = new Dictionary<int, ViewDescriptor>();

        /// <summary>
        /// Register a view via ViewDescriptor
        /// </summary>
        /// <returns> Returns 'true' on success, 'false' otherwise</returns>
        public static bool Register(ViewDescriptor descriptor)
        {
            if (s_ViewDescriptorsRegistry.ContainsKey((int)descriptor.category))
                return false;

            s_ViewDescriptorsRegistry.Add((int)descriptor.category, descriptor);
            return true;
        }

        public static ViewDescriptor[] GetAll()
        {
            return s_ViewDescriptorsRegistry.Select(pair => pair.Value).ToArray();
        }
    }
}
