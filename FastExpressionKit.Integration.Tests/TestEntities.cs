using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastExpressionKit.Integration.Tests
{
    [Table("DB_VERSION_INFO")]
    class TestDbEntity
    {
        [Column("VERSION")]
        public Guid MyId { get; set; }

        [Column("PRODUCT")]
        public string MyString { get; set; }

        [Column("UPDATE_TIME")]
        public DateTime MyDate { get; set; }
    }
}
