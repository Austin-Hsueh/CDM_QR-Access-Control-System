﻿namespace DoorWebApp.Models.DTO
{
    public class ManyTimePermissionDTO
    {
        public string datefrom { get; set; } // 權限日期起
        public string dateto { get; set; } // 權限日期訖
        public string timefrom { get; set; } // 權限時間起
        public string timeto { get; set; } // 權限時間訖
        public List<int> days { get; set; } // 權限一周哪幾天
        public int id { set; get; }
    }

}
