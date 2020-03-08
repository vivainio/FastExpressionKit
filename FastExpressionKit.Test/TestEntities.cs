using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastExpressionKit.Integration.Tests
{
    [Table("DB_VERSION_INFO")]
    public class TestDbEntity
    {
        [Column("VERSION")]
        public Guid MyId { get; set; }

        [Column("PRODUCT")]
        public string MyString { get; set; }

        [Column("UPDATE_TIME")]
        public DateTime MyDate { get; set; }
        
    }
    
    public class TestDbEntityWithoutAnnotations
    {
        public Guid MyId { get; set; }
        public string MyString { get; set; }
        // this collection will be skipped
        public ICollection<string> SomeCollection {
            get;
            set;
        }

        public DateTime MyDate { get; set; }

    }
    
}
