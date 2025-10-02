namespace DoorWebApp.Models.DTO
{
    public class UserPermissionDTO
    {
        public int userId { get; set; }
        public int courseId { get; set; } = 0;
        public int teacherId { get; set; } = 0;
        public string datefrom { get; set; } // 權限日期起
        public string dateto { get; set; } // 權限日期訖
        public string timefrom { get; set; } // 權限時間起
        public string timeto { get; set; } // 權限時間訖
        public int type { get; set; }  // 1=上課 2=租借教室
        public List<int> days { get; set; } // 權限一周哪幾天
        public List<int> groupIds { set; get; }
        
        /// <summary>
        /// 教室Id - 產生課表時使用
        /// </summary>
        public int? classroomId { get; set; }
        
        /// <summary>
        /// 課程模式 1=現場 2=視訊
        /// </summary>
        public int courseMode { get; set; } = 1;
        
        /// <summary>
        /// 排課模式 1=每週固定 2=每兩週固定 3=單次課程
        /// </summary>
        public int scheduleMode { get; set; } = 1;
        
        /// <summary>
        /// 備註
        /// </summary>
        public string? remark { get; set; }
    }

}
