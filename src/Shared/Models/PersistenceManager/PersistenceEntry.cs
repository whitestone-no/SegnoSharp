﻿using System;
using System.Reflection;
using Whitestone.SegnoSharp.Shared.Attributes.PersistenceManager;

namespace Whitestone.SegnoSharp.Shared.Models.PersistenceManager
{
    internal class PersistenceEntry
    {
        public string Key { get; set; }
        public DefaultValueAttribute DefaultValue { get; set; }
        public string DefaultValueString => DefaultValue?.DefaultValue.ToString() ?? string.Empty;
        public object Owner { get; set; }
        public string Value { get; set; }
        public PropertyInfo PropertyInfo { get; set; }
        public Type PropertyType { get; set; }
    }
}
