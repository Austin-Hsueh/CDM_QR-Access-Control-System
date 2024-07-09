using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoorDB.Enums
{
    /// <summary>
    /// 前端頁面的Table類別清單
    /// </summary>
    public enum QRcodeType
    {
        /// <summary>
        /// 未指定
        /// </summary>
        NONE = 0,

        /// <summary>
        /// 一般QRcode
        /// </summary>
        Common = 1,

        /// <summary>
        /// 暫時QRcode
        /// </summary>
        Temporary = 2,

    }
}
