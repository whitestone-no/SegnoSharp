using System;
using System.Reflection;
using Whitestone.SegnoSharp.PersistenceManager.Attributes;

namespace Whitestone.SegnoSharp.PersistenceManager.Models
{
    internal class PersistenceEntry
    {
        public string Key { get; set; }
        public DefaultValueAttribute DefaultValue { get; set; }
        public string DefaultValueString => DefaultValue?.DefaultValue.ToString()?.Replace(',', '.') ?? string.Empty;
        public object Owner { get; set; }
        public string Value { get; set; }
        public PropertyInfo PropertyInfo { get; set; }
        public Type PropertyType { get; set; }
    }
}
