using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DoorDB.Enums;
using SoyalQRGen.Entities.Soyal;

namespace DoorDB
{

    [Table("tblBookingLog")]
    public class TblBookingLog
    {
        /// <summary>
        /// 流水號(Auto increase)
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// UserId
        /// </summary>
        public int UserAddress { get; set; }


        /// <summary>
        /// EventTime
        /// </summary>
        public DateTime EventTime { get; set; }


        /// <summary>
        /// 是否刪除
        /// </summary>
        public bool IsDelete { get; set; }

        /// <summary>
        /// 建立者的UserId([tblUser].[Id])
        /// </summary>
        public int? UpdateUserId { get; set; }

        public TblUser User { set; get; }

        public AccessEventLog AccessEventLog { set; get; }
        
    }
}
