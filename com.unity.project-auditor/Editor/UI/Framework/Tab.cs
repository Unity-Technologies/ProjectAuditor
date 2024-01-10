using System;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    internal enum TabId
    {
        Summary,
        Code,
        Assets,
        Shaders,
        Settings,
        Build,
        BuildData,
    }

    [Serializable]
    internal class Tab
    {
        public TabId id;
        public string name;

        public IssueCategory[] categories;
        public Type[] modules;
        public IssueCategory[] excludedModuleCategories;

        public IssueCategory[] allCategories;
        public IssueCategory[] availableCategories;
        public int currentCategoryIndex;
        public Utility.DropdownItem[] dropdown;
    }
}
