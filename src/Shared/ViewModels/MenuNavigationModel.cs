using System;
using System.Collections.Generic;

namespace Whitestone.SegnoSharp.Shared.ViewModels
{
    public class MenuNavigationModel : BaseMenuNavigation
    {
        public Guid Id { get; set; }
        public bool IsAdmin { get; set; }
        public string Icon { get; set; }
        public List<BaseMenuNavigation> Children { get; set; } = [];
    }

    public class BaseMenuNavigation
    {
        public string MenuTitle { get; set; }
        public int SortOrder { get; set; }
        public string Path { get; set; }
    }
}
