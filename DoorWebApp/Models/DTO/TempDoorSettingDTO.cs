namespace DoorWebApp.Models.DTO
{
    public class TempDoorSettingDTO
    {
        /// <summary>
        /// JWT
        /// </summary>
        public string token { get; set; }

        public string datefrom { get; set; } // 權限日期起
        public string dateto { get; set; } // 權限日期訖
        public string timefrom { get; set; } // 權限時間起
        public string timeto { get; set; } // 權限時間訖
    }

}
