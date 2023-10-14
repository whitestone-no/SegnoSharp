using System.Collections.Generic;
using System.Linq;
using Whitestone.SegnoSharp.Database.Models;

namespace Whitestone.SegnoSharp.Database.Extensions
{
    public static class PersonExtensions
    {
        public static string GetNameString<TParent>(this IEnumerable<BasePersonGroupPersonRelation<TParent>> relations, int groupId)
        {
            if (relations == null)
            {
                return null;
            }

            string names = string.Join(", ", relations
                .FirstOrDefault(r => r.PersonGroup.Id == groupId)?
                .Persons
                .Select(p =>
                {
                    string name = p.LastName;
                    if (p.FirstName != null)
                    {
                        name = p.FirstName + " " + p.LastName;
                    }

                    return name;
                })
                .ToList() ?? new List<string>());

            return string.IsNullOrEmpty(names) ? null : names;
        }
    }
}
