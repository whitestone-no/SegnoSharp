using System;

namespace Whitestone.SegnoSharp.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ModuleMenuAttribute : Attribute
    {
        public string MenuTitle { get; private set; }
        public int SortOrder { get; private set; }
        public string Icon { get; private set; }
        public bool IsAdmin { get; private set; }
        public Type Parent { get; private set; }

        public ModuleMenuAttribute(string menuTitle, bool isAdmin = false)
        {
            MenuTitle = menuTitle;
            SortOrder = default;
            Icon = null;
            IsAdmin = isAdmin;
            Parent = null;
        }
        public ModuleMenuAttribute(string menuTitle, string icon = null, bool isAdmin = false)
        {
            MenuTitle = menuTitle;
            SortOrder = default;
            Icon = icon;
            IsAdmin = isAdmin;
            Parent = null;
        }
        public ModuleMenuAttribute(string menuTitle, int sortOrder, bool isAdmin = false)
        {
            MenuTitle = menuTitle;
            SortOrder = sortOrder;
            Icon = null;
            IsAdmin = isAdmin;
            Parent = null;
        }
        public ModuleMenuAttribute(string menuTitle, int sortOrder, string icon = null, bool isAdmin = false)
        {
            MenuTitle = menuTitle;
            SortOrder = sortOrder;
            Icon = icon;
            IsAdmin = isAdmin;
            Parent = null;
        }

        public ModuleMenuAttribute(string menuTitle, Type parent = null)
        {
            MenuTitle = menuTitle;
            SortOrder = default;
            Icon = null;
            IsAdmin = false;
            Parent = parent;
        }
        public ModuleMenuAttribute(string menuTitle, int sortOrder, Type parent = null)
        {
            MenuTitle = menuTitle;
            SortOrder = sortOrder;
            Icon = null;
            IsAdmin = false;
            Parent = parent;
        }
    }
}
