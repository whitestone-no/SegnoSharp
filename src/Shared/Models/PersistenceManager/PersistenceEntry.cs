using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Whitestone.SegnoSharp.Shared.Attributes.PersistenceManager;

namespace Whitestone.SegnoSharp.Shared.Models.PersistenceManager
{
    internal class PersistenceEntry
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Converters = { new JsonStringEnumConverter() }
        };

        public string Key { get; set; }
        public DefaultValueAttribute DefaultValue { get; set; }
        public string DefaultValueString => JsonSerializer.Serialize(DefaultValue?.DefaultValue, JsonOptions);

        public object Owner { get; set; }
        public string Value { get; set; }
        public PropertyInfo PropertyInfo { get; set; }
        public Type PropertyType { get; set; }
    }
}
