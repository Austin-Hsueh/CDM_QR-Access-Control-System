namespace DoorWebApp.Models.DTO
{
    public class ResStudentTypeCountDTO
    {
        /// <summary>
        /// 在學統計
        /// </summary>
        public int studyingCount { get; set; }


        //// <summary>
        /// 停課統計
        /// </summary>
        public int stopCount { get; set; }

        /// <summary>
        /// 約課統計
        /// </summary>
        public int bookingCount { get; set; }
    }
}
