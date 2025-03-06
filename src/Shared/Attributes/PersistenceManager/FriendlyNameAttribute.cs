using System;

namespace Whitestone.SegnoSharp.Shared.Attributes.PersistenceManager
{
    [AttributeUsage(AttributeTargets.Field)]
    public class FriendlyNameAttribute(string friendlyName) : Attribute
    {
        public string FriendlyName { get; set; } = friendlyName;
    }
}
