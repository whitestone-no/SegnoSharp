using System;

namespace Whitestone.SegnoSharp.Common.Attributes.PersistenceManager
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DescriptionAttribute(string description) : Attribute
    {
        public string Description { get; set; } = description;
    }
}
