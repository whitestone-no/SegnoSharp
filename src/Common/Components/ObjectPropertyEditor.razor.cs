using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Whitestone.SegnoSharp.Common.Attributes.PersistenceManager;

namespace Whitestone.SegnoSharp.Common.Components
{
    public partial class ObjectPropertyEditor
    {
        [Parameter, EditorRequired]
        public object Object { get; set; }

        [Parameter, EditorRequired]
        public string Property { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public EnumDisplay EnumDisplay { get; set; } = EnumDisplay.DropdownList;

        public Type PropertyType => Object.GetType().GetProperty(Property)?.PropertyType;

        private string Identifier => Object.GetType().Name.Replace('.', '-') + "-" + Property;

        public string Description
        {
            get
            {
                if (string.IsNullOrEmpty(Property))
                {
                    return "Property not set";
                }

                PropertyInfo property = Object.GetType().GetProperty(Property);
                if (property == null)
                {
                    return "Non-existing property";
                }

                foreach (object attribute in property.GetCustomAttributes(true))
                {
                    if (attribute is not DescriptionAttribute descriptionAttribute)
                    {
                        continue;
                    }

                    return descriptionAttribute.Description;
                }

                return Property;
            }
        }

        public Dictionary<Enum, string> EnumValues
        {
            get
            {
                Dictionary<Enum, string> enumValues = new();

                PropertyInfo property = Object.GetType().GetProperty(Property);
                if (property == null)
                {
                    return enumValues;
                }

                if (!property.PropertyType.IsEnum)
                {
                    return enumValues;
                }

                foreach (Enum value in Enum.GetValues(property.PropertyType))
                {
                    var friendlyName = value.ToString();

                    MemberInfo[] memberInfos = value.GetType().GetMember(value.ToString());

                    MemberInfo enumValueMemberInfo = memberInfos.FirstOrDefault(m => m.DeclaringType == property.PropertyType);

                    if (enumValueMemberInfo == null)
                    {
                        continue;
                    }

                    foreach (object attribute in enumValueMemberInfo.GetCustomAttributes(true))
                    {
                        if (attribute is not FriendlyNameAttribute friendlyNameAttribute)
                        {
                            continue;
                        }

                        friendlyName = friendlyNameAttribute.FriendlyName;
                        break;
                    }

                    enumValues.Add(value, friendlyName);
                }

                return enumValues;
            }
        }

        public string Value
        {
            get => Object.GetType().GetProperty(Property)?.GetValue(Object)?.ToString();
            set => SetValue(value, Object);
        }

        public bool ValueBool
        {
            get => (bool)Object.GetType().GetProperty(Property)?.GetValue(Object)!;
            set => SetValue(value.ToString(), Object);
        }

        public long ValueNumber
        {
            get => Convert.ToInt64(Object.GetType().GetProperty(Property)?.GetValue(Object)!);
            set => SetValue(value.ToString(), Object);
        }

        public DateTime ValueDateTime
        {
            get => (DateTime)Object.GetType().GetProperty(Property)?.GetValue(Object)!;
            set => SetValue(value.ToString("s"), Object);
        }

        private void SetValue(string value, object configuration)
        {
            PropertyInfo objectProperty = Object.GetType().GetProperty(Property);

            if (objectProperty == null)
            {
                return;
            }

            if (PropertyType == typeof(string))
            {
                objectProperty.SetValue(configuration, value);
            }
            else if (PropertyType == typeof(bool))
            {
                objectProperty.SetValue(configuration, bool.Parse(value));
            }
            else if (PropertyType == typeof(int))
            {
                objectProperty.SetValue(configuration, int.Parse(value));
            }
            else if (PropertyType == typeof(uint))
            {
                objectProperty.SetValue(configuration, uint.Parse(value));
            }
            else if (PropertyType == typeof(ushort))
            {
                objectProperty.SetValue(configuration, ushort.Parse(value));
            }
            else if (PropertyType == typeof(DateTime))
            {
                objectProperty.SetValue(configuration, DateTime.ParseExact(value, "s", CultureInfo.InvariantCulture));
            }
            else if (PropertyType.IsEnum)
            {
                objectProperty.SetValue(configuration, Enum.Parse(PropertyType, value));
            }
        }
    }

    public enum EnumDisplay
    {
        DropdownList,
        RadioButtons
    }
}
