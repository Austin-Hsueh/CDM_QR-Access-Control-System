﻿namespace DoorWebApp.Models.DTO
{
    public class ReqPermissionDTO
    {
        public string datefrom { get; set; } // 權限日期起
        public string dateto { get; set; } // 權限日期訖
        public string timefrom { get; set; } // 權限時間起
        public string timeto { get; set; } // 權限時間訖
        public int[] days { get; set; } // 權限一周哪幾天
        /// <summary>
        /// 該使用者所具有的們Id
        /// </summary>
        public List<int> groupIds { get; set; }
    }

}
