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
    internal class ViewDescriptor
    {
        internal Type type;
        internal IssueCategory category;
        internal string displayName;
        internal string menuLabel;
        internal int menuOrder;
        internal bool descriptionWithIcon;
        internal bool showAssemblySelection;
        internal bool showDependencyView;
        internal bool showFilters;
        internal bool showInfoPanel;
        internal GUIContent dependencyViewGuiContent;
        internal Func<ProjectIssue, string> getAssemblyName;
        internal Action<GenericMenu, ViewManager, ProjectIssue> onContextMenu;
        internal Action<ViewManager> onDrawToolbar;
        internal Action<Location> onOpenIssue;
        internal Action<Descriptor> onOpenManual;
        internal int analyticsEvent;

        static readonly Dictionary<int, ViewDescriptor> s_ViewDescriptorsRegistry = new Dictionary<int, ViewDescriptor>();

        /// <summary>
        /// Register a view via ViewDescriptor
        /// </summary>
        /// <returns> Returns 'true' on success, 'false' otherwise</returns>
        internal static bool Register(ViewDescriptor descriptor)
        {
            if (s_ViewDescriptorsRegistry.ContainsKey((int)descriptor.category))
                return false;

            s_ViewDescriptorsRegistry.Add((int)descriptor.category, descriptor);
            return true;
        }

        internal static ViewDescriptor[] GetAll()
        {
            return s_ViewDescriptorsRegistry.Select(pair => pair.Value).ToArray();
        }
    }
}
