using Microsoft.AspNetCore.Components;
using System.Globalization;
using System;
using System.Reflection;
using Whitestone.SegnoSharp.Common.Attributes.PersistenceManager;

namespace Whitestone.SegnoSharp.Modules.Playlist.Components
{
    public partial class ObjectPropertyEditor
    {
        [Parameter, EditorRequired]
        public object Object { get; set; }

        [Parameter, EditorRequired]
        public PropertyInfo ObjectProperty { get; set; }

        public string Value
        {
            get => ObjectProperty.GetValue(Object)?.ToString();
            set => SetValue(ObjectProperty, value, Object);
        }

        public bool ValueBool
        {
            get => (bool)ObjectProperty.GetValue(Object)!;
            set => SetValue(ObjectProperty, value.ToString(), Object);
        }

        public string Description
        {
            get
            {
                if (ObjectProperty == null)
                {
                    return "Undefined";
                }

                string description = ObjectProperty.Name;

                foreach (object attribute in ObjectProperty.GetCustomAttributes(true))
                {
                    if (attribute is not DescriptionAttribute descriptionAttribute)
                    {
                        continue;
                    }

                    description = descriptionAttribute.Description;
                    break;
                }

                return description;
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        private void SetValue(PropertyInfo propertyInfo, string value, object configuration)
        {
            if (ObjectProperty.PropertyType == typeof(string))
            {
                ObjectProperty.SetValue(configuration, value);
            }
            else if (ObjectProperty.PropertyType == typeof(bool))
            {
                ObjectProperty.SetValue(configuration, bool.Parse(value));
            }
            else if (ObjectProperty.PropertyType == typeof(int))
            {
                ObjectProperty.SetValue(configuration, int.Parse(value));
            }
            else if (ObjectProperty.PropertyType == typeof(float))
            {
                ObjectProperty.SetValue(configuration, float.Parse(value, CultureInfo.InvariantCulture));
            }
            else if (ObjectProperty.PropertyType == typeof(double))
            {
                ObjectProperty.SetValue(configuration, double.Parse(value, CultureInfo.InvariantCulture));
            }
            else if (ObjectProperty.PropertyType == typeof(decimal))
            {
                ObjectProperty.SetValue(configuration, decimal.Parse(value, CultureInfo.InvariantCulture));
            }
            else if (ObjectProperty.PropertyType == typeof(ushort))
            {
                ObjectProperty.SetValue(configuration, ushort.Parse(value, CultureInfo.InvariantCulture));
            }
            else if (ObjectProperty.PropertyType == typeof(DateTime))
            {
                ObjectProperty.SetValue(configuration,
                    DateTime.ParseExact(value, "s", CultureInfo.InvariantCulture));
            }
            else throw new ArgumentException("Unsupported configuration parameter type");
        }

    }
}
