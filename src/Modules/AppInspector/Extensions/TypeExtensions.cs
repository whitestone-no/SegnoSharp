using System;
using System.Linq;

namespace Whitestone.SegnoSharp.Modules.AppInspector.Extensions
{
    internal static class TypeExtensions
    {
        internal static string GetTypeName(this Type type)
        {
            string typeName = type.Namespace + "." + type.Name;
            if (typeName.Contains('`'))
            {
                typeName = typeName[..typeName.LastIndexOf('`')];
            }

            if (!type.IsGenericType)
            {
                return typeName;
            }

            typeName += "<";

            if (type.GenericTypeArguments.Any())
            {
                typeName += type.GenericTypeArguments[0].GetTypeName();
            }

            typeName += ">";

            return typeName;
        }
    }
}
