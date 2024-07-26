using System.ComponentModel.DataAnnotations;

namespace Whitestone.SegnoSharp.Database.Models
{
    public class PersistenceManagerEntry
    {
        [Key]
        public string Key { get; set; }
        public string Value { get; set; }
        public string DataType { get; set; }
    }
}
