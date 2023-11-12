using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Whitestone.SegnoSharp.Database.Interfaces;

namespace Whitestone.SegnoSharp.Database.Models
{
    [Index(nameof(LastName), nameof(FirstName))]
    public class Person : IEquatable<Person>, IComparable<Person>, ITag
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string LastName { get; set; }
        [StringLength(100)]
        public string FirstName { get; set; }

        public ushort Version { get; set; }

        public IList<AlbumPersonGroupPersonRelation> AlbumPersonGroupPersonRelations { get; set; }
        public IList<TrackPersonGroupPersonRelation> TrackPersonGroupPersonRelations { get; set; }

        public bool Equals(Person other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id &&
                   LastName == other.LastName &&
                   FirstName == other.FirstName &&
                   Version == other.Version;
        }

        public int CompareTo(Person other)
        {
            if (other == null) return 1;

            if (LastName == other.LastName)
            {
                if (FirstName == other.FirstName)
                {
                    if (Version == other.Version)
                    {
                        return 0;
                    }

                    return Version.CompareTo(other.Version);
                }

                return string.Compare(FirstName, other.FirstName, StringComparison.Ordinal);
            }

            return string.Compare(LastName, other.LastName, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Person)obj);
        }

        public override int GetHashCode()
        {
            // ReSharper disable NonReadonlyMemberInGetHashCode
            return HashCode.Combine(Id, LastName, FirstName, Version);
            // ReSharper restore NonReadonlyMemberInGetHashCode
        }

        public static bool operator ==(Person left, Person right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(Person left, Person right)
        {
            return !(left == right);
        }

        public static bool operator <(Person left, Person right)
        {
            return ReferenceEquals(left, null) ? !ReferenceEquals(right, null) : left.CompareTo(right) < 0;
        }

        public static bool operator <=(Person left, Person right)
        {
            return ReferenceEquals(left, null) || left.CompareTo(right) <= 0;
        }

        public static bool operator >(Person left, Person right)
        {
            return !ReferenceEquals(left, null) && left.CompareTo(right) > 0;
        }

        public static bool operator >=(Person left, Person right)
        {
            return ReferenceEquals(left, null) ? ReferenceEquals(right, null) : left.CompareTo(right) >= 0;
        }

        [NotMapped]
        public string TagName
        {
            get
            {
                var name = string.Empty;

                if (FirstName != null)
                {
                    name = FirstName + " ";
                }

                name += LastName;
                if (Version > 0)
                {
                    name += " (" + Version + ")";
                }
                return name;
            }
            set
            {
                value = value.Trim();
                string lastname = value;
                string firstname = null;

                int paranthesesStartIndex = value.LastIndexOf('(');
                int paranthesesEndIndex = value.LastIndexOf(')');

                int firstCommaIndex = value.IndexOf(',');
                if (firstCommaIndex != -1)
                {
                    lastname = value[..firstCommaIndex].Trim();
                    firstname = value[(firstCommaIndex + 1)..].Trim();
                }
                else
                {
                    int lastSpaceIndex = value.LastIndexOf(' ');

                    if (paranthesesStartIndex != -1)
                    {
                        lastSpaceIndex = value.LastIndexOf(' ', paranthesesStartIndex - 2);
                    }
                    if (lastSpaceIndex != -1)
                    {
                        firstname = value[..lastSpaceIndex].Trim();
                        lastname = value[lastSpaceIndex..].Trim();
                    }
                }

                FirstName = FilterOutParantheses(firstname, out ushort version);
                if (version > 0)
                {
                    Version = version;
                }
                LastName = FilterOutParantheses(lastname, out version);
                if (version > 0)
                {
                    Version = version;
                }
            }
        }

        public static string FilterOutParantheses(string value, out ushort valueWithinParantheses)
        {
            if (value == null)
            {
                valueWithinParantheses = 0;
                return null;
            }

            int paranthesesStartIndex = value.LastIndexOf('(');
            int paranthesesEndIndex = value.LastIndexOf(')');

            if (paranthesesStartIndex == -1 || paranthesesEndIndex == -1)
            {
                valueWithinParantheses = 0;
                return value;
            }

            string valueWithin = value[(paranthesesStartIndex + 1)..paranthesesEndIndex];
            _ = ushort.TryParse(valueWithin, out valueWithinParantheses);

            return value[..(paranthesesStartIndex - 1)];
        }
    }
}
