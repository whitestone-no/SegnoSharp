using System;

namespace Whitestone.SegnoSharp.Shared.Attributes.PersistenceManager
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DescriptionAttribute(string description) : Attribute
    {
        public string Description { get; set; } = description;
    }
}
