using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoorDB.Enums
{
    public enum LoginAccountType
    {
        /// <summary>
        /// 未指定
        /// </summary>
        NONE = 0,

        /// <summary>
        /// 本機DB帳號
        /// </summary>
        LOCAL = 1,

        /// <summary>
        /// 來自Windows AD的帳號
        /// </summary>
        LDAP = 2,
    }
}
