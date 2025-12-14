using System.ComponentModel.DataAnnotations;

namespace DoorWebApp.Models.DTO
{
    /// <summary>
    /// 新增學生權限費用請求 DTO
    /// </summary>
    public class ReqCreateStudentPermissionFeeDTO
    {
        /// <summary>
        /// 學生權限 ID (必填)
        /// </summary>
        [Required]
        public int StudentPermissionId { get; set; }
    }
}
