using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    internal class ViewDescriptor
    {
        public Type Type;
        public IssueCategory Category;
        public string DisplayName;
        public string MenuLabel;
        public int MenuOrder;
        public bool DescriptionWithIcon;
        public bool ShowAssemblySelection;
        public bool ShowDependencyView;
        public bool ShowFilters;
        public bool ShowInfoPanel;
        public bool ShowAdditionalInfoPanel;
        public GUIContent DependencyViewGuiContent;
        public Func<ReportItem, string> GetAssemblyName;
        public Action<GenericMenu, ViewManager, ReportItem> OnContextMenu;
        public Action<ViewManager> OnDrawToolbar;
        public Action<Location> OnOpenIssue;
        public Action<Descriptor> OnOpenManual;
        public int AnalyticsEventId;

        static readonly Dictionary<int, ViewDescriptor> s_ViewDescriptorsRegistry = new Dictionary<int, ViewDescriptor>();

        /// <summary>
        /// Register a view via ViewDescriptor
        /// </summary>
        /// <returns> Returns 'true' on success, 'false' otherwise</returns>
        public static bool Register(ViewDescriptor descriptor)
        {
            if (s_ViewDescriptorsRegistry.ContainsKey((int)descriptor.Category))
                return false;

            s_ViewDescriptorsRegistry.Add((int)descriptor.Category, descriptor);
            return true;
        }

        public static ViewDescriptor[] GetAll()
        {
            return s_ViewDescriptorsRegistry.Select(pair => pair.Value).ToArray();
        }
    }
}
