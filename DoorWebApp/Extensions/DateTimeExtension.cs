namespace DoorWebApp.Extensions
{
    public static class DateTimeExtension
    {
        /// <summary>
        /// 將日期月份轉為整數，e.g. 2022-08-15 -> 202208
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static int ToMonthInteger(this DateTime dateTime)
        { 
            int MonthInt = dateTime.Year * 100 + dateTime.Month;
            return MonthInt;
        }


        /// <summary>
        /// 將月份數值轉為DateTime物件(e.g. 202208 -> 2022/08/01 00:00:00)
        /// </summary>
        /// <param name="MonthInt"></param>
        /// <returns></returns>
        public static DateTime ToDateTime(this int MonthInteger)
        {
            int year = MonthInteger / 100;
            int month = MonthInteger % 100;
            return new DateTime(year, month, 1, 0, 0, 0);
        }
    }
}
