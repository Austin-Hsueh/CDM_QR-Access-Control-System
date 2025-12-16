using System.ComponentModel.DataAnnotations;

namespace DoorWebApp.Models.DTO
{
    /// <summary>
    /// 新增或編輯關帳請求 DTO
    /// </summary>
    public class SaveCloseAccountRequest
    {
        /// <summary>
        /// 關帳日期 (格式: yyyy-MM-dd)
        /// </summary>
        [Required]
        public string Date { get; set; }

        /// <summary>
        /// 提存金額
        /// </summary>
        [Required]
        public int DepositAmount { get; set; }
    }
}
