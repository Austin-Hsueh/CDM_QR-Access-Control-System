using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoorDB.Enums
{
    /// <summary>
    /// Audit行為清單
    /// </summary>
    public enum AuditActType
    {
        None    = -1,
        Login   = 1,
        Logout  = 2,
        LoginFail = 3,

        Create  = 10,
        Modify  = 11,
        Delete  = 12,
    }
}
