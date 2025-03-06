using System;

namespace Whitestone.SegnoSharp.Shared.Attributes.PersistenceManager
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DefaultValueAttribute : Attribute
    {
        public object DefaultValue { get; set; }

        internal DefaultValueAttribute() { }

        public DefaultValueAttribute(string defaultValue)
        {
            DefaultValue = defaultValue;
        }

        public DefaultValueAttribute(int defaultValue)
        {
            DefaultValue = defaultValue;
        }

        public DefaultValueAttribute(DateTime defaultValue)
        {
            DefaultValue = defaultValue;
        }

        public DefaultValueAttribute(float defaultValue)
        {
            DefaultValue = defaultValue;
        }

        public DefaultValueAttribute(bool defaultValue)
        {
            DefaultValue = defaultValue;
        }

        public DefaultValueAttribute(Type defaultValue)
        {
            DefaultValue = defaultValue;
        }
    }
}
