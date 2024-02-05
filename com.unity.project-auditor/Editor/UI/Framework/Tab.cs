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
    }

    [Serializable]
    internal class Tab
    {
        public TabId id;
        public string name;

        public IssueCategory[] categories;

        public int currentCategoryIndex;
#if !PA_DRAW_TABS_VERTICALLY
        public Utility.DropdownItem[] dropdown;
#endif
    }
}
