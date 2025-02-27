using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Whitestone.SegnoSharp.Common.Attributes.PersistenceManager;

namespace Whitestone.SegnoSharp.Common.Components
{
    public partial class ObjectEditor
    {
        [Parameter, EditorRequired]
        public object Object { get; set; }

        [Parameter]
        public Type MarkerAttribute { get; set; } = typeof(PersistAttribute);

        [Parameter]
        public string[] IgnoreProperties { get; set; } = [];

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public EnumDisplay EnumDisplay { get; set; } = EnumDisplay.DropdownList;

        private List<PropertyInfo> Properties
        {
            get
            {
                List<PropertyInfo> properties = [];

                foreach (PropertyInfo propertyInfo in Object.GetType().GetProperties())
                {
                    if (IgnoreProperties.Contains(propertyInfo.Name))
                    {
                        continue;
                    }

                    object[] attributes = propertyInfo.GetCustomAttributes(true);

                    foreach (object attribute in attributes)
                    {
                        if (attribute.GetType() != MarkerAttribute)
                        {
                            continue;
                        }

                        properties.Add(propertyInfo);
                    }
                }

                return properties;
            }
        }
    }
}
