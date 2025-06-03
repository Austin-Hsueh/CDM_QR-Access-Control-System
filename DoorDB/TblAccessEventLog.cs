using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoorDB
{
    [Table("tblAccessEventLog")]
    public class AccessEventLog
    {
        [Key]
        public string Id { get; set; } = null!;
        public int NodeId { get; set; }
        public int FunctionCode { get; set; }
        public DateTime EventTime { get; set; }
        public int PortNumber { get; set; }
        public int UserAddress { get; set; }
        public int DoorNumber { get; set; }
        public int CardId1 { get; set; }
        public int CardId2 { get; set; }
    }
} 