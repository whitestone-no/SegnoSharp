using System;

namespace Whitestone.SegnoSharp.Common.Attributes.PersistenceManager
{
    [AttributeUsage(AttributeTargets.Field)]
    public class FriendlyNameAttribute(string friendlyName) : Attribute
    {
        public string FriendlyName { get; set; } = friendlyName;
    }
}
